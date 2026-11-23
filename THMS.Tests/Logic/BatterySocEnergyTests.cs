using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.Logic.Transportation;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class BatterySocEnergyTests
    {
        [Test]
        public void Fit_IgnoresCalculatedKwhAndZeroSocChange()
        {
            var sessions = new BaseEvChargeSession[]
            {
                new()
                {
                    StartSoc = 20,
                    EndSoc = 60,
                    KwhAdded = 32
                },
                new()
                {
                    StartSoc = 10,
                    EndSoc = 20,
                    KwhAdded = 100,
                    KwhAddedIsCalculated = true
                },
                new()
                {
                    StartSoc = 40,
                    EndSoc = 40,
                    KwhAdded = 15
                }
            };

            var fit = BatterySocEnergy.Fit(sessions);
            Assert.That(fit, Is.Not.Null);
            Assert.That(fit!.Value.Coefficients, Is.EqualTo(new SocEnergyCoefficients(0.8m, 0, 0)));
            Assert.That(fit.Value.HigherTermsUsed, Is.False);
        }

        [Test]
        public void Fit_WeightsSessionsBySocChange()
        {
            var sessions = new BaseEvChargeSession[]
            {
                new() { StartSoc = 0, EndSoc = 40, KwhAdded = 32 },
                new() { StartSoc = 50, EndSoc = 60, KwhAdded = 9 }
            };

            var fit = BatterySocEnergy.Fit(sessions);
            Assert.That(fit, Is.Not.Null);
            Assert.That(fit!.Value.Coefficients.A1, Is.EqualTo(1370m / 1700m));
            Assert.That(fit.Value.Coefficients.A2, Is.Zero);
            Assert.That(fit.Value.Coefficients.A3, Is.Zero);
        }

        [Test]
        public void Fit_IgnoresZeroKwh()
        {
            var sessions = new BaseEvChargeSession[]
            {
                new() { StartSoc = 20, EndSoc = 60, KwhAdded = 32 },
                new() { StartSoc = 0, EndSoc = 100, KwhAdded = 0 }
            };

            var fit = BatterySocEnergy.Fit(sessions);
            Assert.That(fit!.Value.Coefficients, Is.EqualTo(new SocEnergyCoefficients(0.8m, 0, 0)));
        }

        [Test]
        public void Fit_ReturnsNullWhenEveryValueIsCalculated()
        {
            var sessions = new[]
            {
                new BaseEvChargeSession
                {
                    StartSoc = 10,
                    EndSoc = 40,
                    KwhAdded = 25,
                    KwhAddedIsCalculated = true
                }
            };

            Assert.That(BatterySocEnergy.Fit(sessions), Is.Null);
        }

        [Test]
        public void Estimate_UsesThePolynomial()
        {
            var linear = new SocEnergyCoefficients(0.835m, 0, 0);
            Assert.That(BatterySocEnergy.EstimateKwh(32, 80, linear), Is.EqualTo(40.080m));
            Assert.That(BatterySocEnergy.EstimateKwh(80, 32, linear), Is.Null);
            Assert.That(BatterySocEnergy.EstimateKwh(20, 50, null), Is.Null);

            var curved = new SocEnergyCoefficients(0.5m, 0.001m, 0);
            Assert.That(BatterySocEnergy.EstimateKwh(10, 20, curved), Is.EqualTo(5.300m));
        }

        [Test]
        public void Fit_HoldsHigherTermsAtZeroWithoutEnoughExtremeSessions()
        {
            var fit = BatterySocEnergy.Fit(CurvedSessions(4))!.Value;
            Assert.That(fit.ExtremeSessions, Is.EqualTo(4));
            Assert.That(fit.HigherTermsUsed, Is.False);
            Assert.That(fit.Coefficients.A2, Is.Zero);
            Assert.That(fit.Coefficients.A3, Is.Zero);
        }

        [Test]
        public void Fit_UsesCubicWhenEnoughSessionsReachTheEnds()
        {
            const decimal a1 = 0.7m;
            const decimal a2 = 0.0004m;
            const decimal a3 = -0.000002m;
            var fit = BatterySocEnergy.Fit(CurvedSessions(5, a1, a2, a3))!.Value;

            Assert.That(fit.HigherTermsUsed, Is.True);
            Assert.That(fit.ExtremeSessions, Is.EqualTo(5));
            Assert.That(fit.Coefficients.A1, Is.EqualTo(a1).Within(0.0000001m));
            Assert.That(fit.Coefficients.A2, Is.EqualTo(a2).Within(0.000000001m));
            Assert.That(fit.Coefficients.A3, Is.EqualTo(a3).Within(0.00000000001m));
        }

        [Test]
        public void Fit_IgnoresCurvatureInsideTheMiddleBand()
        {
            var bands = new[] { (30m, 50m), (40m, 70m), (25m, 80m), (50m, 90m), (20m, 60m), (35m, 75m) };
            var sessions = bands.Select(band => new BaseEvChargeSession
            {
                StartSoc = band.Item1,
                EndSoc = band.Item2,
                KwhAdded = 0.7m * (band.Item2 - band.Item1)
                    + 0.0004m * (band.Item2 * band.Item2 - band.Item1 * band.Item1)
            }).ToArray();

            var fit = BatterySocEnergy.Fit(sessions)!.Value;
            Assert.That(fit.ExtremeSessions, Is.Zero);
            Assert.That(fit.HigherTermsUsed, Is.False);
            Assert.That(fit.Coefficients.A2, Is.Zero);
            Assert.That(fit.Coefficients.A3, Is.Zero);
        }

        [Test]
        public void Recalculate_StoresTheRateFromActualSessionsOnly()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var ev = vehicles.GetAllVehicles().OfType<VehicleEv>().First();
            vehicles.UpsertBaseEvChargeSession(Session(ev.Id, 20, 60, 32, calculated: false));
            vehicles.UpsertBaseEvChargeSession(Session(ev.Id, 10, 90, 200, calculated: true));

            var orchestrator = new EvChargeSessionOrchestrator(
                vehicles,
                new InMemoryEnergyDataStore(),
                new InMemoryFinanceDataStore());

            var fit = orchestrator.RecalculateChargeEnergy(ev.Id);

            Assert.That(fit, Is.Not.Null);
            Assert.That(fit!.Value.Coefficients, Is.EqualTo(new SocEnergyCoefficients(0.8m, 0, 0)));
            Assert.That(((VehicleEv)vehicles.GetVehicle(ev.Id)!).ChargeEnergy, Is.EqualTo(new SocEnergyCoefficients(0.8m, 0, 0)));

            Assert.That(orchestrator.RecalculateChargeEnergy(Guid.NewGuid()), Is.Null);
        }

        [Test]
        public void Recalculate_RefreshesCalculatedBatteryChargeWithoutRewritingTheSession()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var ev = vehicles.GetAllVehicles().OfType<VehicleEv>().First();
            vehicles.UpsertBaseEvChargeSession(Session(ev.Id, 20, 60, 32, calculated: false));

            var calculated = Session(ev.Id, 10, 50, 1, calculated: true);
            calculated.VehicleName = "Original";
            calculated.LastOdometer = 100;
            calculated.KwhDrawn = 7.5m;
            calculated.SessionCost = 3m;
            vehicles.UpsertBaseEvChargeSession(calculated);
            var separateCopy = new CommercialEvChargeSession
            {
                Id = calculated.Id,
                VehicleId = ev.Id,
                VehicleName = "Original",
                StartSoc = 10,
                EndSoc = 50,
                KwhAdded = 1,
                KwhAddedIsCalculated = true,
                LastOdometer = 100,
                KwhDrawn = 7.5m,
                SessionCost = 3m
            };
            vehicles.UpsertCommercialEvChargeSession(separateCopy);

            var orchestrator = new EvChargeSessionOrchestrator(
                vehicles,
                new InMemoryEnergyDataStore(),
                new InMemoryFinanceDataStore());

            var fit = orchestrator.RecalculateChargeEnergy(ev.Id);

            Assert.That(fit!.Value.RefreshedCalculatedSessions, Is.EqualTo(1));
            Assert.That(fit.Value.Coefficients.A1, Is.EqualTo(0.8m));

            var stored = vehicles.GetBaseEvChargeSession(calculated.Id);
            Assert.That(stored!.KwhAdded, Is.EqualTo(32m));
            Assert.That(stored.KwhAddedIsCalculated, Is.True);
            Assert.That(stored.VehicleName, Is.EqualTo("Original"));
            Assert.That(stored.LastOdometer, Is.EqualTo(100m));
            Assert.That(stored.StartSoc, Is.EqualTo(10m));

            var commercial = vehicles.GetCommercialEvChargeSession(calculated.Id);
            Assert.That(commercial!.KwhAdded, Is.EqualTo(32m));
            Assert.That(commercial.KwhDrawn, Is.EqualTo(7.5m));
            Assert.That(commercial.SessionCost, Is.EqualTo(3m));
            Assert.That(commercial.VehicleName, Is.EqualTo("Original"));
        }

        [Test]
        public void Sqlite_RoundTripsCalculatedFlagAndCoefficient()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-soc-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteVehicleDataStore(path);
                var ev = new VehicleEv
                {
                    Id = Guid.NewGuid(),
                    Name = "Fit",
                    Make = "Ford",
                    Model = "Mach-E",
                    Year = 2023,
                    BatteryCapacityKwh = 91,
                    ChargeEnergy = new SocEnergyCoefficients(0.835m, 0.0002m, -0.000001m)
                };
                store.UpsertVehicle(ev);
                var session = new CommercialEvChargeSession
                {
                    Id = Guid.NewGuid(),
                    VehicleId = ev.Id,
                    VehicleName = ev.Name,
                    StartTime = new DateTime(2026, 6, 1, 8, 0, 0),
                    EndTime = new DateTime(2026, 6, 1, 10, 0, 0),
                    StartSoc = 30,
                    EndSoc = 80,
                    KwhAdded = 41.75m,
                    KwhAddedIsCalculated = true,
                    OdometerMiles = 1000
                };
                store.UpsertBaseEvChargeSession(session);
                store.UpsertCommercialEvChargeSession(session);

                var reloaded = new SQLiteVehicleDataStore(path);
                var vehicle = reloaded.GetVehicle(ev.Id) as VehicleEv;
                var stored = reloaded.GetBaseEvChargeSessions(ev.Id, DateTime.MinValue, DateTime.MaxValue).Single();
                var commercial = reloaded.GetCommercialEvChargeSession(session.Id);

                Assert.That(vehicle!.ChargeEnergy!.Value.A1, Is.EqualTo((decimal)(double)0.835m));
                Assert.That(vehicle.ChargeEnergy.Value.A2, Is.EqualTo((decimal)(double)0.0002m));
                Assert.That(vehicle.ChargeEnergy.Value.A3, Is.EqualTo((decimal)(double)(-0.000001m)));
                Assert.That(stored.KwhAdded, Is.EqualTo(41.75m));
                Assert.That(stored.KwhAddedIsCalculated, Is.True);
                Assert.That(commercial!.KwhAddedIsCalculated, Is.True);
                Assert.That(commercial.StartSoc, Is.EqualTo(30));

                reloaded.UpsertEvBatteryCharge(session.Id, new EvBatteryCharge
                {
                    KwhAdded = 55.5m,
                    IsCalculated = true
                });
                var afterChargeUpdate = reloaded.GetBaseEvChargeSession(session.Id);
                Assert.That(afterChargeUpdate!.KwhAdded, Is.EqualTo(55.5m));
                Assert.That(afterChargeUpdate.StartSoc, Is.EqualTo(30m));
                Assert.That(afterChargeUpdate.KwhAddedIsCalculated, Is.True);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                {
                    var file = path + suffix;
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        private static readonly (decimal Start, decimal End)[] ExtremeBands =
        [
            (5, 30),
            (8, 40),
            (12, 60),
            (18, 92),
            (25, 96)
        ];

        private static BaseEvChargeSession[] CurvedSessions(
            int count,
            decimal a1 = 0.7m,
            decimal a2 = 0.0004m,
            decimal a3 = -0.000002m) =>
            ExtremeBands.Take(count).Select(band => new BaseEvChargeSession
            {
                StartSoc = band.Start,
                EndSoc = band.End,
                KwhAdded = a1 * (band.End - band.Start)
                    + a2 * (band.End * band.End - band.Start * band.Start)
                    + a3 * (band.End * band.End * band.End - band.Start * band.Start * band.Start)
            }).ToArray();

        private static CommercialEvChargeSession Session(
            Guid vehicleId,
            decimal start,
            decimal end,
            decimal kwh,
            bool calculated) =>
            new()
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                StartTime = DateTime.Today,
                EndTime = DateTime.Today.AddHours(1),
                StartSoc = start,
                EndSoc = end,
                KwhAdded = kwh,
                KwhAddedIsCalculated = calculated
            };
    }
}
