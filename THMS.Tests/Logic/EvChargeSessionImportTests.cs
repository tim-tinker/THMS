using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Ingestion.Importers.Transportation;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class SpreadsheetEvChargeSessionImporterTests
    {
        [Test]
        public void Parse_MapsHomeAndCommercialRows_AndSkipsIncomplete()
        {
            var path = WriteTempCsv(
                "Date,Charger (kW),Odometer,Start SOC,Start time,End time,End SOC,kWh Added,battery kwh,Cost Added\n" +
                "2026-03-01,Home,12345,0.26,20:00:00,06:00:00,0.80,,91,0\n" +
                "2026-03-02,150,12400,0.20,12:00:00,12:30:00,0.80,45,91,18.50\n" +
                "2026-03-03,Home,12500,0.30,18:00:00,,0.40,,91,0\n" +
                "46273,6,12600,26,08:00:00,09:15:00,40,12,91,4.25\n");

            try
            {
                var rows = new SpreadsheetEvChargeSessionImporter().Parse(path);
                Assert.That(rows, Has.Count.EqualTo(3));

                var home = rows[0];
                Assert.That(home.IsHomeCharge, Is.True);
                Assert.That(home.StartTime, Is.EqualTo(new DateTime(2026, 3, 1, 20, 0, 0)));
                Assert.That(home.EndTime, Is.EqualTo(new DateTime(2026, 3, 2, 6, 0, 0)));
                Assert.That(home.StartSoc, Is.EqualTo(26m));
                Assert.That(home.EndSoc, Is.EqualTo(80m));
                Assert.That(home.KwhDrawn, Is.EqualTo(0m));
                Assert.That(home.KwhAdded, Is.EqualTo(91m));
                Assert.That(home.CostAdded, Is.EqualTo(0m));

                var commercial = rows[1];
                Assert.That(commercial.IsHomeCharge, Is.False);
                Assert.That(commercial.Charger, Is.EqualTo("150"));
                Assert.That(commercial.KwhDrawn, Is.EqualTo(45m));
                Assert.That(commercial.KwhAdded, Is.EqualTo(91m));
                Assert.That(commercial.CostAdded, Is.EqualTo(18.50m));

                var oaDate = rows[2];
                Assert.That(oaDate.StartTime.Date, Is.EqualTo(DateTime.FromOADate(46273).Date));
                Assert.That(oaDate.StartSoc, Is.EqualTo(26m));
                Assert.That(oaDate.IsHomeCharge, Is.False);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Parse_MissingFile_Throws()
        {
            var importer = new SpreadsheetEvChargeSessionImporter();
            var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.csv");
            Assert.That(() => importer.Parse(missing), Throws.TypeOf<FileNotFoundException>());
        }

        private static string WriteTempCsv(string contents)
        {
            var path = Path.Combine(Path.GetTempPath(), $"ev-charge-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, contents);
            return path;
        }
    }

    [TestFixture]
    public class EvChargeSessionImportOrchestratorTests
    {
        [Test]
        public void LoadAndImport_PersistsHomeAndCommercial_AndSkipsDuplicates()
        {
            var (orchestrator, vehicles, vehicle) = CreateOrchestrator();
            var path = WriteTempCsv(
                "Date,Charger (kW),Odometer,Start SOC,Start time,End time,End SOC,kWh Added,battery kwh,Cost Added\n" +
                "2026-03-01,Home,12345,0.26,20:00:00,06:00:00,0.80,,91,0\n" +
                "2026-03-02,150,12400,0.20,12:00:00,12:30:00,0.80,45,12.5,18.50\n");

            try
            {
                var preview = orchestrator.LoadSessionsFromFiles([path], vehicle);
                Assert.That(preview, Has.Count.EqualTo(2));
                Assert.That(preview[0].StartTime, Is.GreaterThan(preview[1].StartTime));

                var home = preview.Single(row => row.IsHomeCharge);
                var commercial = preview.Single(row => !row.IsHomeCharge);
                Assert.That(home.LastOdometer, Is.EqualTo(12345m));
                Assert.That(home.LastSoc, Is.EqualTo(26m));
                Assert.That(home.KwhDrawn, Is.EqualTo(0m));
                Assert.That(home.KwhAdded, Is.EqualTo(91m));
                Assert.That(home.SessionCost, Is.EqualTo(0m));
                Assert.That(commercial.LastOdometer, Is.EqualTo(12345m));
                Assert.That(commercial.LastSoc, Is.EqualTo(80m));
                Assert.That(commercial.KwhDrawn, Is.EqualTo(45m));
                Assert.That(commercial.KwhAdded, Is.EqualTo(12.5m));
                Assert.That(commercial.SessionCost, Is.EqualTo(18.50m));

                Assert.That(orchestrator.ImportSessions(preview).Count, Is.EqualTo(2));
                var stored = vehicles.GetBaseEvChargeSessions(vehicle.Id, DateTime.MinValue, DateTime.MaxValue).ToList();
                Assert.That(stored, Has.Count.EqualTo(2));
                Assert.That(stored[0].IsHomeCharge, Is.True);
                Assert.That(stored[0].KwhAdded, Is.EqualTo(91m));
                Assert.That(stored[0].KwhDrawn, Is.EqualTo(0m));
                Assert.That(stored[1].IsHomeCharge, Is.False);
                Assert.That(stored[1].KwhDrawn, Is.EqualTo(45m));
                Assert.That(stored[1].KwhAdded, Is.EqualTo(12.5m));
                Assert.That(vehicles.GetHomeEvChargeSession(stored[0].Id), Is.Not.Null);
                Assert.That(vehicles.GetCommercialEvChargeSession(stored[1].Id), Is.Not.Null);
                Assert.That(vehicles.GetCommercialEvChargeSession(stored[1].Id)!.SessionCost, Is.EqualTo(18.50m));

                var again = orchestrator.LoadSessionsFromFiles([path], vehicle);
                Assert.That(orchestrator.ImportSessions(again).Count, Is.EqualTo(0));
                Assert.That(
                    vehicles.GetBaseEvChargeSessions(vehicle.Id, DateTime.MinValue, DateTime.MaxValue).Count(),
                    Is.EqualTo(2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void LoadSessionsFromFiles_UsesExistingPriorSessionForLastOdometer()
        {
            var (orchestrator, vehicles, vehicle) = CreateOrchestrator();
            vehicles.UpsertBaseEvChargeSession(new HomeEvChargeSession
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicle.Id,
                VehicleName = vehicle.Name,
                StartTime = new DateTime(2026, 2, 1, 20, 0, 0),
                EndTime = new DateTime(2026, 2, 2, 6, 0, 0),
                OdometerMiles = 12000,
                EndSoc = 90,
                StartSoc = 20,
                LastOdometer = 11900,
                LastSoc = 20,
                KwhAdded = 40
            });

            var path = WriteTempCsv(
                "Date,Charger (kW),Odometer,Start SOC,Start time,End time,End SOC,kWh Added,Cost Added\n" +
                "2026-03-01,Home,12345,0.26,20:00:00,06:00:00,0.80,,\n");

            try
            {
                var preview = orchestrator.LoadSessionsFromFiles([path], vehicle);
                Assert.That(preview, Has.Count.EqualTo(1));
                Assert.That(preview[0].LastOdometer, Is.EqualTo(12000m));
                Assert.That(preview[0].LastSoc, Is.EqualTo(90m));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void LoadSessionsFromFiles_MissingFile_Throws()
        {
            var (orchestrator, _, vehicle) = CreateOrchestrator();
            var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.csv");
            Assert.That(
                () => orchestrator.LoadSessionsFromFiles([missing], vehicle),
                Throws.TypeOf<FileNotFoundException>());
        }

        [Test]
        public void GetEvVehicles_ReturnsEvOnly()
        {
            var (orchestrator, _, _) = CreateOrchestrator();
            Assert.That(orchestrator.GetEvVehicles().All(v => v is VehicleEv), Is.True);
            Assert.That(orchestrator.GetEvVehicles(), Has.Count.GreaterThanOrEqualTo(1));
        }

        private static (EvChargeSessionImportOrchestrator Orchestrator, InMemoryVehicleDataStore Vehicles, VehicleEv Vehicle)
            CreateOrchestrator()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var sessions = new EvChargeSessionOrchestrator(
                vehicles,
                new InMemoryEnergyDataStore(),
                new InMemoryFinanceDataStore());
            var orchestrator = new EvChargeSessionImportOrchestrator(
                vehicles,
                new SpreadsheetEvChargeSessionImporter(),
                sessions);
            var vehicle = vehicles.GetAllVehicles().OfType<VehicleEv>().Single();
            return (orchestrator, vehicles, vehicle);
        }

        private static string WriteTempCsv(string contents)
        {
            var path = Path.Combine(Path.GetTempPath(), $"ev-charge-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, contents);
            return path;
        }
    }
}
