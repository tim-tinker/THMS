using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Finance;
using THMS.Domain.Transportation;
using THMS.Logic.DataCenter;
using THMS.Logic.ViewModels;
using THMS.Tests.Logic.TestSupport;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class StatusModelTests
    {
        [Test]
        public void StatusModels_ExposeSettableProperties()
        {
            var solar = new SolarDataStatus
            {
                HasData = true,
                LastInterval = DateTime.Today,
                IsPartialMonth = true,
                IsMissingMonth = true,
                ExpectedAvailableDate = DateTime.Today
            };
            Assert.That(solar.HasData, Is.True);

            var bill = new BillDataStatus
            {
                HasData = true,
                LastBillDate = DateTime.Today,
                IsMissingMonth = false,
                ExpectedAvailableDate = DateTime.Today
            };
            Assert.That(bill.LastBillDate, Is.EqualTo(DateTime.Today));

            var circuit = new HomeCircuitStatus { HasData = true, LastReading = DateTime.Today, HasGaps = true };
            Assert.That(circuit.HasGaps, Is.True);

            var ev = new EvCommercialStatus { HasData = true, LastSession = DateTime.Today, HasMissingSessions = true };
            Assert.That(ev.HasMissingSessions, Is.True);

            var attr = new AttributionStatus { HasData = true, LastAttribution = DateTime.Today, NeedsRecalculation = true };
            Assert.That(attr.NeedsRecalculation, Is.True);
        }
    }

    [TestFixture]
    public class DataSourceStatusTests
    {
        [Test]
        public void SolarStatus_UsesLatestIntervalOrToday()
        {
            var store = new InMemoryEnergyDataStore();
            var status = new SolarDataSourceStatus(store);
            Assert.That(status.DataSourceName, Is.EqualTo("Solar Data"));

            status.QueryStatus();
            Assert.That(status.LastRetrieval, Is.Null);
            Assert.That(status.NextExpectedRetrieval.Date, Is.EqualTo(DateTime.Today));

            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = new DateTime(2026, 1, 15)
            });
            status.QueryStatus();
            Assert.That(status.LastRetrieval, Is.EqualTo(new DateTime(2026, 1, 15)));
            Assert.That(status.NextExpectedRetrieval, Is.EqualTo(new DateTime(2026, 2, 15)));
        }

        [Test]
        public void ElectricContractStatus_ReadyWhenExpired()
        {
            var store = new InMemoryFinanceDataStore();
            var status = new ElectricContractDataSourceStatus(store);
            Assert.That(status.DataSourceName, Is.EqualTo("Electric Contract"));
            status.QueryStatus();
            Assert.That(status.IsReadyForUpdate, Is.True);

            store.UpsertElectricContract(new ElectricContract
            {
                Id = Guid.NewGuid(),
                Name = "Future",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10)
            });
            status.QueryStatus();
            Assert.That(status.IsReadyForUpdate, Is.False);
            Assert.That(status.LastRetrieval, Is.EqualTo(DateTime.Today.AddDays(10)));
        }

        [Test]
        public void EvAndCircuitStatuses_ReadLatest()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var energy = new InMemoryEnergyDataStore();
            var evStatus = new EvChargeSessionDataSourceStatus(vehicles);
            var circuitStatus = new HomeCircuitReadingDataSourceStatus(energy);

            Assert.That(evStatus.DataSourceName, Is.EqualTo("EV Charge Sessions"));
            Assert.That(circuitStatus.DataSourceName, Is.EqualTo("EV Circuit Readings"));

            evStatus.QueryStatus();
            circuitStatus.QueryStatus();
            Assert.That(evStatus.LastRetrieval, Is.Null);
            Assert.That(circuitStatus.LastRetrieval, Is.Null);

            var ev = vehicles.GetAllVehicles().OfType<VehicleEv>().First();
            vehicles.UpsertBaseEvChargeSession(new CommercialEvChargeSession
            {
                VehicleId = ev.Id,
                EndTime = new DateTime(2026, 2, 2)
            });
            energy.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = new DateTime(2026, 2, 3)
            });

            evStatus.QueryStatus();
            circuitStatus.QueryStatus();
            Assert.That(evStatus.LastRetrieval, Is.EqualTo(new DateTime(2026, 2, 2)));
            Assert.That(circuitStatus.LastRetrieval, Is.EqualTo(new DateTime(2026, 2, 3)));
        }

        [Test]
        public void AttributionStatus_ReadyWhenReadingsAndSolarAreNewer()
        {
            var store = new InMemoryEnergyDataStore();
            var status = new HomeCircuitAttributionDataSourceStatus(store);
            Assert.That(status.DataSourceName, Is.EqualTo("EV Attribution"));

            status.QueryStatus();
            Assert.That(status.LastRetrieval, Is.Null);
            Assert.That(status.IsReadyForUpdate, Is.False);

            store.UpsertHomeCircuitAttribution(new HomeCircuitAttribution { Timestamp = new DateTime(2026, 1, 1) });
            store.UpsertHomeCircuitReading(new HomeCircuitReading { Timestamp = new DateTime(2026, 1, 2) });
            store.UpsertSolarProductionInterval(new SolarProductionInterval { Timestamp = new DateTime(2026, 1, 3) });
            status.QueryStatus();
            Assert.That(status.LastRetrieval, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(status.IsReadyForUpdate, Is.True);
        }
    }

    [TestFixture]
    public class DataAvailabilityServiceTests
    {
        [Test]
        public void GetAvailability_QueriesInjectedStatuses()
        {
            var queried = false;
            var status = new RecordingStatus(() => queried = true);
            var vm = new DataAvailabilityService([status]).GetAvailability();
            Assert.That(queried, Is.True);
            Assert.That(vm._dataSourceStatuses, Has.Count.EqualTo(1));
        }

        private sealed class RecordingStatus : IDataSourceStatus
        {
            private readonly Action _onQuery;
            public RecordingStatus(Action onQuery) => _onQuery = onQuery;
            public string DataSourceName => "Rec";
            public DateTime? LastRetrieval => null;
            public void QueryStatus() => _onQuery();
        }
    }

    [TestFixture]
    public class DataCenterViewModelTests
    {
        [Test]
        public void AddDataSourceStatuses_AppendsAndHoldsCommands()
        {
            var vm = new DataCenterViewModel
            {
                SolarStatus = new SolarDataStatus(),
                BillStatus = new BillDataStatus(),
                HomeCircuitStatus = new HomeCircuitStatus(),
                EvCommercialStatus = new EvCommercialStatus(),
                AttributionStatus = new AttributionStatus()
            };
            vm.AddDataSourceStatuses([new SolarDataSourceStatus(new InMemoryEnergyDataStore())]);
            Assert.That(vm._dataSourceStatuses, Has.Count.EqualTo(1));
            Assert.That(vm.SolarStatus, Is.Not.Null);
        }
    }
}
