using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;
using THMS.Logic.Transportation;
using THMS.Tests.Logic.TestSupport;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class MpgEngineTests
    {
        [Test]
        public void ComputeMpg_AggregatesPartialsIntoNextFullFillUp()
        {
            var vehicleId = Guid.NewGuid();
            var records = new[]
            {
                new IceMileageRecord
                {
                    VehicleId = vehicleId,
                    EndTime = new DateTime(2026, 1, 1),
                    OdometerMiles = 1000,
                    GallonsAdded = 10,
                    IsFullFillUp = true
                },
                new IceMileageRecord
                {
                    VehicleId = vehicleId,
                    EndTime = new DateTime(2026, 1, 10),
                    OdometerMiles = 1200,
                    GallonsAdded = 4,
                    IsFullFillUp = false
                },
                new IceMileageRecord
                {
                    VehicleId = vehicleId,
                    EndTime = new DateTime(2026, 1, 20),
                    OdometerMiles = 1400,
                    GallonsAdded = 8,
                    IsFullFillUp = true
                }
            };

            var engine = new MpgEngine();
            var results = engine.ComputeMpg(records).ToList();
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].MilesDriven, Is.EqualTo(400));
            Assert.That(results[0].GallonsUsed, Is.EqualTo(12));
            Assert.That(engine.ComputeAverageMpg(records), Is.EqualTo(400m / 12m));
        }

        [Test]
        public void ComputeAverageMpg_NoFullFillPairs_ReturnsZero()
        {
            var engine = new MpgEngine();
            Assert.That(engine.ComputeAverageMpg([]), Is.EqualTo(0));
            Assert.That(
                engine.ComputeMonthlyMpg(
                    [new IceMileageRecord { EndTime = new DateTime(2026, 2, 1), IsFullFillUp = true }],
                    new DateTime(2026, 1, 1),
                    new DateTime(2026, 1, 31)),
                Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class MpgeEngineTests
    {
        [Test]
        public void Compute_FewerThanTwoMileageRecords_ReturnsZeros()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var energy = new InMemoryEnergyDataStore();
            var vehicleId = Guid.NewGuid();
            vehicles.UpsertIceMileageRecord(new IceMileageRecord
            {
                VehicleId = vehicleId,
                EndTime = new DateTime(2026, 1, 1),
                OdometerMiles = 10
            });

            var result = new MpgeEngine(vehicles, energy).Compute(
                vehicleId, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            Assert.That(result.MilesDriven, Is.EqualTo(0));
            Assert.That(result.WhUsed, Is.EqualTo(0));
            Assert.That(result.VehicleId, Is.EqualTo(vehicleId));
        }

        [Test]
        public void Compute_UsesStoredAttribution_AndZeroMpgeWhenNoEnergy()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var energy = new InMemoryEnergyDataStore();
            var vehicleId = Guid.NewGuid();
            vehicles.UpsertIceMileageRecord(new IceMileageRecord
            {
                VehicleId = vehicleId,
                EndTime = new DateTime(2026, 1, 1),
                OdometerMiles = 100
            });
            vehicles.UpsertIceMileageRecord(new IceMileageRecord
            {
                VehicleId = vehicleId,
                EndTime = new DateTime(2026, 1, 15),
                OdometerMiles = 200
            });

            var zeroEnergy = new MpgeEngine(vehicles, energy).Compute(
                vehicleId, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
            Assert.That(zeroEnergy.MilesDriven, Is.EqualTo(100));
            Assert.That(zeroEnergy.WhUsed, Is.EqualTo(0));

            energy.UpsertHomeCircuitAttribution(new HomeCircuitAttribution
            {
                Timestamp = new DateTime(2026, 1, 5),
                TotalWh = 33700
            });

            var withEnergy = new MpgeEngine(vehicles, energy).Compute(
                vehicleId, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
            Assert.That(withEnergy.WhUsed, Is.EqualTo(33700));
        }
    }

    [TestFixture]
    public class TransportationAnalyticsEngineTests
    {
        [Test]
        public void ComputeMonthlySummary_AndLifetimeCostPerMile()
        {
            var store = new InMemoryVehicleDataStore();
            var ev = store.GetAllVehicles().OfType<VehicleEv>().First();
            var ice = store.GetAllVehicles().OfType<VehicleIce>().First();
            var start = new DateTime(2026, 5, 1);
            var end = new DateTime(2026, 5, 20);

            store.UpsertCommercialEvChargeSession(new CommercialEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = start,
                EndTime = start.AddHours(1),
                OdometerMiles = 1000,
                SessionCost = 12
            });
            store.UpsertBaseEvChargeSession(new CommercialEvChargeSession
            {
                Id = Guid.NewGuid(),
                VehicleId = ev.Id,
                StartTime = start,
                EndTime = start.AddHours(1),
                OdometerMiles = 1000,
                SessionCost = 12
            });
            store.UpsertBaseEvChargeSession(new HomeEvChargeSession
            {
                Id = Guid.NewGuid(),
                VehicleId = ev.Id,
                StartTime = start.AddDays(1),
                EndTime = start.AddDays(1).AddHours(2),
                OdometerMiles = 1100,
                Billing = new HomeEvChargeBilling { SessionCost = 3 }
            });
            store.UpsertHomeEvChargeBilling(
                store.GetBaseEvChargeSessions(ev.Id, start, end).OfType<HomeEvChargeSession>().First().Id,
                new HomeEvChargeBilling { SessionCost = 3 });

            var home = store.GetBaseEvChargeSessions(ev.Id, DateTime.MinValue, DateTime.MaxValue)
                .OfType<HomeEvChargeSession>()
                .First();
            home.Billing = new HomeEvChargeBilling { SessionCost = 3 };
            store.UpsertBaseEvChargeSession(home);

            store.UpsertIceMileageRecord(new IceMileageRecord
            {
                VehicleId = ice.Id,
                EndTime = start,
                OdometerMiles = 500,
                FuelCost = 40
            });
            store.UpsertIceMileageRecord(new IceMileageRecord
            {
                VehicleId = ice.Id,
                EndTime = end,
                OdometerMiles = 700,
                FuelCost = 20
            });
            store.UpsertMaintenanceInvoice(new MaintenanceInvoiceRecord
            {
                VehicleId = ice.Id,
                Date = start.AddDays(2),
                Cost = 80
            });

            var engine = new TransportationAnalyticsEngine(store);
            var monthly = engine.ComputeMonthlySummary(ice.Id, 2026, 5);
            Assert.That(monthly.FuelCost, Is.EqualTo(60));
            Assert.That(monthly.MaintenanceCost, Is.EqualTo(80));

            var all = engine.ComputeAllMonthlySummaries(2026, 5);
            Assert.That(all, Has.Count.EqualTo(2));

            var lifetimeIce = engine.ComputeLifetimeCostPerMile(ice.Id);
            Assert.That(lifetimeIce, Is.GreaterThan(0));

            var unknown = engine.ComputeLifetimeCostPerMile(Guid.NewGuid());
            Assert.That(unknown, Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class TransportationCostAggregatorTests
    {
        [Test]
        public void GetCostSummary_ThrowsWhenVehicleMissing()
        {
            var aggregator = new TransportationCostAggregator(
                new InMemoryVehicleDataStore(),
                new InMemoryFinanceDataStore());

            Assert.That(
                () => aggregator.GetCostSummary(Guid.NewGuid(), DateTime.Today, DateTime.Today),
                Throws.InvalidOperationException.With.Message.Contain("Vehicle not found"));
        }

        [Test]
        public void GetCostSummary_ThrowsForUnknownVehicleType()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var unknown = new UnknownVehicle { Id = Guid.NewGuid(), Name = "X" };
            vehicles.UpsertVehicle(unknown);

            var aggregator = new TransportationCostAggregator(vehicles, new InMemoryFinanceDataStore());
            Assert.That(
                () => aggregator.GetCostSummary(unknown.Id, DateTime.Today, DateTime.Today),
                Throws.InvalidOperationException.With.Message.Contain("Unknown vehicle type"));
        }

        [Test]
        public void GetCostSummary_IceUsesFuelCostAndOdometerDelta()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var ice = vehicles.GetAllVehicles().OfType<VehicleIce>().First();
            vehicles.UpsertIceMileageRecord(new IceMileageRecord
            {
                VehicleId = ice.Id,
                EndTime = new DateTime(2026, 1, 1),
                OdometerMiles = 100,
                FuelCost = 30
            });
            vehicles.UpsertIceMileageRecord(new IceMileageRecord
            {
                VehicleId = ice.Id,
                EndTime = new DateTime(2026, 1, 10),
                OdometerMiles = 200,
                FuelCost = 20
            });

            var summary = new TransportationCostAggregator(vehicles, new InMemoryFinanceDataStore())
                .GetCostSummary(ice.Id, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            Assert.That(summary, Is.TypeOf<THMS.Domain.Finance.IceTransportationCostSummary>());
            var iceSummary = (THMS.Domain.Finance.IceTransportationCostSummary)summary;
            Assert.That(iceSummary.TotalMiles, Is.EqualTo(100));
            Assert.That(iceSummary.FuelCost, Is.EqualTo(50));
            Assert.That(iceSummary.CostPerMile, Is.EqualTo(0.5m));
        }

        [Test]
        public void GetCostSummary_IceWithNoRecords_ZeroMiles()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var ice = new VehicleIce { Id = Guid.NewGuid(), Name = "Empty ICE" };
            vehicles.UpsertVehicle(ice);

            var summary = (THMS.Domain.Finance.IceTransportationCostSummary)
                new TransportationCostAggregator(vehicles, new InMemoryFinanceDataStore())
                    .GetCostSummary(ice.Id, DateTime.Today, DateTime.Today.AddDays(1));

            Assert.That(summary.TotalMiles, Is.EqualTo(0));
            Assert.That(summary.CostPerMile, Is.EqualTo(0));
        }

        [Test]
        public void GetCostSummary_EvUsesBillsAndSessions()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var finance = new InMemoryFinanceDataStore();
            var ev = vehicles.GetAllVehicles().OfType<VehicleEv>().First();
            var start = new DateTime(2026, 6, 17);
            var end = new DateTime(2026, 7, 17);

            var homeId = Guid.NewGuid();
            vehicles.UpsertBaseEvChargeSession(new HomeEvChargeSession
            {
                Id = homeId,
                VehicleId = ev.Id,
                StartTime = start.AddDays(1),
                EndTime = start.AddDays(1).AddHours(2),
                OdometerMiles = 1000,
                KwhDrawn = 20
            });
            vehicles.UpsertBaseEvChargeSession(new CommercialEvChargeSession
            {
                Id = Guid.NewGuid(),
                VehicleId = ev.Id,
                StartTime = start.AddDays(2),
                EndTime = start.AddDays(2).AddHours(1),
                OdometerMiles = 1100,
                SessionCost = 15
            });

            var summary = (THMS.Domain.Finance.EvTransportationCostSummary)
                new TransportationCostAggregator(vehicles, finance)
                    .GetCostSummary(ev.Id, start, end);

            Assert.That(summary.CommercialChargeCost, Is.EqualTo(15));
            Assert.That(summary.HomeChargeCost, Is.GreaterThan(0));
            Assert.That(summary.TotalMiles, Is.EqualTo(100));
        }

        [Test]
        public void GetCostSummary_EvWithoutBills_HomeCostIsZero()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var finance = new InMemoryFinanceDataStore();
            var ev = new VehicleEv { Id = Guid.NewGuid(), Name = "Solo EV", BatteryCapacityKwh = 70 };
            vehicles.UpsertVehicle(ev);

            var summary = (THMS.Domain.Finance.EvTransportationCostSummary)
                new TransportationCostAggregator(vehicles, finance)
                    .GetCostSummary(ev.Id, new DateTime(2020, 1, 1), new DateTime(2020, 1, 31));

            Assert.That(summary.HomeChargeCost, Is.EqualTo(0));
            Assert.That(summary.TotalMiles, Is.EqualTo(0));
            Assert.That(summary.CostPerMile, Is.EqualTo(0));
        }

        [Test]
        public void GetCostSummary_EvBillWithZeroKwh_DoesNotDivideByZero()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var finance = new InMemoryFinanceDataStore();
            var ev = new VehicleEv { Id = Guid.NewGuid(), Name = "Zero kWh EV" };
            vehicles.UpsertVehicle(ev);
            var start = new DateTime(2026, 11, 1);
            var end = new DateTime(2026, 11, 30);

            finance.UpsertElectricUtilityBill(new ElectricUtilityBill
            {
                Id = Guid.NewGuid(),
                StartDate = start,
                EndDate = end,
                KwhUsage = 0,
                EnergyCharge = 10,
                BaseCharge = 0
            });
            vehicles.UpsertBaseEvChargeSession(new HomeEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = start.AddDays(1),
                EndTime = start.AddDays(1).AddHours(1),
                KwhDrawn = 5
            });

            var summary = (THMS.Domain.Finance.EvTransportationCostSummary)
                new TransportationCostAggregator(vehicles, finance)
                    .GetCostSummary(ev.Id, start, end);

            Assert.That(summary.HomeChargeCost, Is.EqualTo(50));
        }
    }

    [TestFixture]
    public class EmptyTransportationEngineTests
    {
        [Test]
        public void InternalEmptyTypes_CanBeConstructed()
        {
            Assert.That(new CostPerMileCalculator(), Is.Not.Null);
            Assert.That(new TransportCostEngine(), Is.Not.Null);
        }
    }
}
