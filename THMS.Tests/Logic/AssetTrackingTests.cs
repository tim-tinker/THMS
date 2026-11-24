using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Transportation;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class AssetTrackingTests
    {
        [Test]
        public void PropertyTaxStatement_RecordsBillValuesAndValuationHistory()
        {
            var orchestrator = Orchestrator();
            var collector = orchestrator.EnsureTaxCollector("Fort Bend Tax Office");

            var property = new PropertyAsset
            {
                Name = "6219 Cresting Knolls",
                Category = "Real estate",
                ParcelAddress = "6219 Cresting Knolls Cir",
                CadNumber = "R424196",
                TaxAccountNumber = "5121-32-001-0130-901",
                LegalDescription = "Lonf Meadow Farms Sec 3",
                LegalAcres = 0m,
                RollCode = "Real Property"
            };
            var statement = CrestingKnollsStatement(collector.Id);
            orchestrator.SaveAsset(property, null, null);
            var appraisal = new PropertyAppraisal
            {
                AsOf = statement.StatementDate,
                Source = "CAD notice",
                LandMarketValue = statement.LandMarketValue,
                ImprovementMarketValue = statement.ImprovementMarketValue
            };
            orchestrator.SavePropertyAppraisal(property, appraisal);
            statement.ValuationId = appraisal.Id;
            orchestrator.SavePropertyTaxStatement(property, statement);

            var saved = orchestrator.GetPropertyTaxStatement(statement.Id);
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.AmountDue, Is.EqualTo(8135.25m));
            Assert.That(saved.Jurisdictions, Has.Count.EqualTo(4));
            Assert.That(saved.AssessedValue, Is.EqualTo(627100m));
            Assert.That(saved.Exemptions, Is.EqualTo("Homestead, Veteran"));
            Assert.That(saved.ValuationId, Is.EqualTo(appraisal.Id));

            var values = orchestrator.GetValuations(property.Id);
            Assert.That(values, Has.Count.EqualTo(5));
            Assert.That(values.Single(item => item.ValueType == ValuationTypes.TotalMarket).Amount, Is.EqualTo(627100m));
            Assert.That(values.Single(item => item.ValueType == ValuationTypes.LandMarket).AsOf, Is.EqualTo(new DateTime(2026, 10, 26)));
            Assert.That(values.All(item => item.AppraisalId == appraisal.Id), Is.True);

            statement.LandMarketValue = 85000m;
            statement.TotalMarketValue = 627600m;
            statement.AssessedValue = 627600m;
            MatchLevy(statement);
            orchestrator.SavePropertyTaxStatement(property, statement);

            values = orchestrator.GetValuations(property.Id);
            Assert.That(values.Single(item => item.ValueType == ValuationTypes.LandMarket).Amount, Is.EqualTo(84500m));
            Assert.That(orchestrator.GetPropertyTaxStatement(statement.Id)!.LandMarketValue, Is.EqualTo(85000m));

            appraisal.LandMarketValue = 85000m;
            orchestrator.SavePropertyAppraisal(property, appraisal);
            var corrected = orchestrator.GetPropertyAppraisals(property.Id).Single();
            Assert.That(corrected.LandMarketValue, Is.EqualTo(85000m));
            Assert.That(corrected.TotalMarketValue, Is.EqualTo(627600m));
            Assert.That(orchestrator.GetPropertyTaxStatement(statement.Id)!.LandMarketValue, Is.EqualTo(85000m));
        }

        [Test]
        public void RecordedValuation_CanBeCorrected()
        {
            var orchestrator = Orchestrator();
            var property = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(property, null, null);
            var appraisal = new PropertyAppraisal
            {
                AsOf = new DateTime(2026, 10, 26),
                LandMarketValue = 84500m,
                ImprovementMarketValue = 542600m
            };
            orchestrator.SavePropertyAppraisal(property, appraisal);

            appraisal.LandMarketValue = 90000m;
            orchestrator.SavePropertyAppraisal(property, appraisal);

            var saved = orchestrator.GetPropertyAppraisals(property.Id).Single();
            Assert.That(saved.LandMarketValue, Is.EqualTo(90000m));
            Assert.That(saved.TotalMarketValue, Is.EqualTo(632600m));
            Assert.That(saved.AssessedValue, Is.EqualTo(632600m));
            Assert.That(orchestrator.GetValuations(property.Id), Has.Count.EqualTo(5));
            Assert.That(orchestrator.GetValuations(property.Id).All(item => item.SourceStatementId is null), Is.True);
        }

        [Test]
        public void PropertyTaxStatement_RejectsATaxThatDoesNotMatchTheRate()
        {
            var statement = CrestingKnollsStatement(Guid.NewGuid());
            statement.AssetId = Guid.NewGuid();
            statement.AmountDue = 8135.25m;
            statement.Jurisdictions[0].TaxAmount = 5000m;

            var findings = AccountStatementValidator.Validate(statement);

            Assert.That(findings, Has.Some.Contains("Lamar CISD"));
        }

        [Test]
        public void PropertyTax_ApplyHomesteadCap_SubtractsTheCapFromMarket()
        {
            var statement = Bill(AssessmentMethod.ApplyHomesteadCap);
            statement.LandMarketValue = 84500m;
            statement.ImprovementMarketValue = 542600m;
            statement.TotalMarketValue = 627100m;
            statement.HomesteadCap = 27000m;
            statement.AssessedValue = 600100m;

            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);

            statement.HomesteadCap = 700000m;
            statement.AssessedValue = 0m;
            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);

            statement.AssessedValue = 1m;
            Assert.That(AccountStatementValidator.Validate(statement), Has.Some.Contains("homestead cap"));
        }

        [Test]
        public void PropertyTax_EqualMarketValue_RequiresAssessedToMatchTotal()
        {
            var statement = Bill(AssessmentMethod.EqualMarketValue);
            statement.HomesteadCap = 27000m;
            statement.AssessedValue = statement.TotalMarketValue;

            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);

            statement.AssessedValue = statement.TotalMarketValue - 1m;
            Assert.That(AccountStatementValidator.Validate(statement), Has.Some.Contains("total market"));
        }

        [Test]
        public void PropertyTax_AssessmentRatio_MultipliesTotalByTheRatio()
        {
            var statement = Bill(AssessmentMethod.AssessmentRatio);
            statement.AssessmentRatioPercent = 40m;
            statement.AssessedValue = 250840m;
            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);

            statement.TotalMarketValue = 100m;
            statement.LandMarketValue = 40m;
            statement.ImprovementMarketValue = 60m;
            statement.AssessmentRatioPercent = 33.3333m;
            statement.AssessedValue = 33.33m;
            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);

            statement.AssessedValue = 33.30m;
            Assert.That(AccountStatementValidator.Validate(statement), Has.Some.Contains("assessment ratio"));

            statement.AssessmentRatioPercent = 0m;
            statement.AssessedValue = 0m;
            Assert.That(AccountStatementValidator.Validate(statement), Has.Some.Contains("Assessment ratio"));
        }

        [Test]
        public void PropertyTax_RatePerHundred_SubtractsExemptionsAndAppliesTheRate()
        {
            var statement = CrestingKnollsStatement(Guid.NewGuid());
            statement.AssetId = Guid.NewGuid();
            statement.AmountDue = 8135.25m;
            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);

            statement.Jurisdictions[0].ExemptionAmount = 700000m;
            statement.Jurisdictions[0].TaxableValue = 0m;
            statement.Jurisdictions[0].TaxAmount = 0m;
            statement.AmountDue = 8135.25m - 5491.12m;
            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);

            statement.Jurisdictions[0].TaxableValue = 1m;
            Assert.That(AccountStatementValidator.Validate(statement), Has.Some.Contains("taxable value"));
        }

        [Test]
        public void PropertyTax_Millage_DividesByOneThousand()
        {
            var statement = CrestingKnollsStatement(Guid.NewGuid());
            statement.AssetId = Guid.NewGuid();
            statement.LevyMethod = LevyMethod.Millage;
            MatchLevy(statement);
            statement.AmountDue = statement.Jurisdictions.Sum(line => line.TaxAmount);

            Assert.That(statement.Jurisdictions[0].TaxAmount, Is.EqualTo(549.11m));
            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);
        }

        [Test]
        public void PropertyTax_ManualLevy_AllowsValuesThatDoNotFollowTheRate()
        {
            var statement = CrestingKnollsStatement(Guid.NewGuid());
            statement.AssetId = Guid.NewGuid();
            statement.LevyMethod = LevyMethod.Manual;
            statement.AmountDue = 8135.25m;
            statement.Jurisdictions[0].TaxableValue = 10m;
            statement.Jurisdictions[0].TaxAmount = 3m;
            statement.AmountDue = statement.Jurisdictions.Sum(line => line.TaxAmount);

            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);
        }

        [Test]
        public void PropertyTax_Manual_AllowsValuesThatDoNotFollowAFormula()
        {
            var statement = Bill(AssessmentMethod.Manual);
            statement.TotalMarketValue = 500000m;
            statement.HomesteadCap = 1000m;
            statement.AssessedValue = 123456m;

            Assert.That(AccountStatementValidator.Validate(statement), Is.Empty);
        }

        [Test]
        public void GetBills_ListsEveryOpenPropertyTaxStatement()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var collector = new UntrackedAccount
            {
                Name = "County",
                Institution = "County",
                AccountNumber = "",
                WebsiteUrl = "",
                Type = AccountType.Tax
            };
            accounts.UpsertAccount(collector);
            statements.Save(TaxBill(collector.Id, new DateTime(2025, 10, 1), new DateTime(2026, 1, 31), 1000m));
            statements.Save(TaxBill(collector.Id, new DateTime(2026, 10, 26), new DateTime(2027, 1, 31), 8135.25m));

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(collector.Id, new DateTime(2026, 11, 1));

            Assert.That(rows.Select(row => row.Amount), Is.EquivalentTo(new[] { 1000m, 8135.25m }));
            Assert.That(rows.All(row => row.Kind == BillKinds.Statement), Is.True);
        }

        [Test]
        public void Sqlite_RoundTripsPropertyVehicleLoanAndMortgageLinks()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-assets-{Guid.NewGuid():N}.db");
            try
            {
                var assets = new SQLiteAssetDataStore(path);
                var accounts = new SQLiteAccountDataStore(path);
                var statements = new SQLiteAccountStatementDataStore(path);
                var transactions = new InMemoryTransactionDataStore();
                var vehicles = new InMemoryVehicleDataStore();
                var orchestrator = new AssetOrchestrator(assets, accounts, statements, transactions, vehicles);

                var house = new PropertyAsset { Name = "House", ParcelAddress = "1 Main", CadNumber = "R1" };
                var mortgage = new MortgageAccount
                {
                    Name = "Mortgage",
                    Institution = "Bank",
                    AccountNumber = "9",
                    WebsiteUrl = "",
                    Type = AccountType.Mortgage,
                    NextPaymentDate = new DateTime(2026, 12, 1)
                };
                accounts.UpsertAccount(mortgage);
                orchestrator.SaveAsset(house, mortgage.Id, vehicle: null);

                var car = new VehicleIce { Year = 2018, Make = "Ford", Model = "Escape", Vin = "VIN123" };
                var vehicleAsset = new VehicleAsset { Name = "Escape", Category = "Vehicle", CostBasis = 18000m };
                var loan = new LoanAccount
                {
                    Name = "Auto loan",
                    Institution = "Bank",
                    AccountNumber = "8",
                    WebsiteUrl = "",
                    Type = AccountType.Loan
                };
                accounts.UpsertAccount(loan);
                orchestrator.SaveAsset(vehicleAsset, loan.Id, car);

                var reloadedAssets = new SQLiteAssetDataStore(path);
                var reloadedAccounts = new SQLiteAccountDataStore(path);
                var property = reloadedAssets.GetAll().OfType<PropertyAsset>().Single();
                var vehicle = reloadedAssets.GetAll().OfType<VehicleAsset>().Single();
                Assert.That(property.CadNumber, Is.EqualTo("R1"));
                Assert.That(vehicle.VehicleId, Is.EqualTo(car.Id));
                Assert.That(reloadedAccounts.GetAccount("Mortgage"), Is.TypeOf<MortgageAccount>());
                Assert.That(((MortgageAccount)reloadedAccounts.GetAccount("Mortgage")!).SecuredAssetId, Is.EqualTo(property.Id));
                Assert.That(((LoanAccount)reloadedAccounts.GetAccount("Auto loan")!).SecuredAssetId, Is.EqualTo(vehicle.Id));

                var collector = orchestrator.EnsureTaxCollector("County Tax");
                var statement = CrestingKnollsStatement(collector.Id);
                var appraisal = new PropertyAppraisal
                {
                    AsOf = statement.StatementDate,
                    LandMarketValue = statement.LandMarketValue,
                    ImprovementMarketValue = statement.ImprovementMarketValue
                };
                orchestrator.SavePropertyAppraisal(property, appraisal);
                statement.ValuationId = appraisal.Id;
                orchestrator.SavePropertyTaxStatement(property, statement);
                var reloadedStatements = new SQLiteAccountStatementDataStore(path);
                var tax = (PropertyTaxStatement)reloadedStatements.Get(statement.Id)!;
                Assert.That(tax.Jurisdictions, Has.Count.EqualTo(4));
                Assert.That(tax.TaxYear, Is.EqualTo(2026));
                Assert.That(tax.AssessmentMethod, Is.EqualTo(AssessmentMethod.ApplyHomesteadCap));
                Assert.That(tax.AssessmentRatioPercent, Is.EqualTo(100m));
                Assert.That(tax.LevyMethod, Is.EqualTo(LevyMethod.RatePerHundred));
                Assert.That(tax.ValuationId, Is.EqualTo(appraisal.Id));
                Assert.That(property.LevyMethod, Is.EqualTo(LevyMethod.RatePerHundred));
                var values = reloadedAssets.GetValuations(property.Id);
                Assert.That(values, Has.Count.EqualTo(5));
                Assert.That(values.All(item => item.AppraisalId == appraisal.Id), Is.True);
            }
            finally
            {
                TryDelete(path);
            }
        }

        [Test]
        public void Sqlite_RoundTripsAssessmentRatio()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-ratio-{Guid.NewGuid():N}.db");
            try
            {
                var orchestrator = new AssetOrchestrator(
                    new SQLiteAssetDataStore(path),
                    new SQLiteAccountDataStore(path),
                    new SQLiteAccountStatementDataStore(path),
                    new InMemoryTransactionDataStore(),
                    new InMemoryVehicleDataStore());
                var property = new PropertyAsset
                {
                    Name = "Ratio house",
                    Category = "Real estate",
                    AssessmentMethod = AssessmentMethod.AssessmentRatio
                };
                var collector = orchestrator.EnsureTaxCollector("County");
                var statement = CrestingKnollsStatement(collector.Id);
                statement.AssessmentRatioPercent = 40m;
                statement.AssessedValue = 250840m;
                MatchLevy(statement);
                orchestrator.SavePropertyTaxStatement(property, statement);

                var savedProperty = new SQLiteAssetDataStore(path).GetAll().OfType<PropertyAsset>().Single();
                var saved = (PropertyTaxStatement)new SQLiteAccountStatementDataStore(path).Get(statement.Id)!;
                Assert.That(savedProperty.AssessmentMethod, Is.EqualTo(AssessmentMethod.AssessmentRatio));
                Assert.That(saved.AssessmentMethod, Is.EqualTo(AssessmentMethod.AssessmentRatio));
                Assert.That(saved.AssessmentRatioPercent, Is.EqualTo(40m));
                Assert.That(saved.AssessedValue, Is.EqualTo(250840m));
                Assert.That(saved.HomesteadCap, Is.EqualTo(0m));
            }
            finally
            {
                TryDelete(path);
            }
        }

        [Test]
        public void EnsureVehicleAssets_LinksTheExistingVehicleWithoutMovingFueling()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var ice = vehicles.GetAllVehicles().OfType<VehicleIce>().Single();
            var fillUp = new IceMileageRecord
            {
                VehicleId = ice.Id,
                VehicleName = ice.Name,
                EndTime = new DateTime(2026, 3, 1),
                OdometerMiles = 12000,
                GallonsAdded = 11,
                IsFullFillUp = true,
                FuelCost = 40
            };
            vehicles.UpsertIceMileageRecord(fillUp);
            vehicles.UpsertVehicle(new VehicleIce
            {
                Id = Guid.NewGuid(),
                Year = 2014,
                Make = "Honda",
                Model = "Fit"
            });

            var assets = new InMemoryAssetDataStore();
            assets.Save(new PersonalAsset { Name = ice.Name, Category = "Personal" });
            var orchestrator = new AssetOrchestrator(
                assets,
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                vehicles);

            var rows = orchestrator.GetAssetRows();
            orchestrator.EnsureVehicleAssets();

            Assert.That(rows.Select(row => row.Name), Is.EquivalentTo(new[]
            {
                ice.Name, "Julie's (2)", "Tim's", "2014 Honda Fit"
            }));
            var linked = assets.GetAll().OfType<VehicleAsset>().Single(asset => asset.Name == "Julie's (2)");
            Assert.That(linked.VehicleId, Is.EqualTo(ice.Id));
            Assert.That(vehicles.GetVehicle(ice.Id)!.Id, Is.EqualTo(ice.Id));
            Assert.That(vehicles.GetEarliestIceMileageRecord(ice.Id)!.VehicleId, Is.EqualTo(ice.Id));
            Assert.That(assets.GetAll().OfType<VehicleAsset>().Count(asset => asset.VehicleId == ice.Id), Is.EqualTo(1));

            AssetOrchestrator.RenameLinkedVehicle(assets, ice.Id, "Julie's Escape");
            Assert.That(assets.GetAll().OfType<VehicleAsset>().Single(asset => asset.VehicleId == ice.Id).Name,
                Is.EqualTo("Julie's Escape"));
        }

        [Test]
        public void StatementAccountMatch_TreatsATaxCollectorAsAPropertyTaxAccount()
        {
            var collector = new UntrackedAccount { Type = AccountType.Tax };
            Assert.That(StatementAccountMatch.ForAccount(collector), Is.EqualTo(StatementType.PropertyTax));
            Assert.That(StatementAccountMatch.CreateStatement(StatementType.PropertyTax), Is.TypeOf<PropertyTaxStatement>());
            Assert.That(StatementAccountMatch.CreateAccount(StatementType.PropertyTax).Type, Is.EqualTo(AccountType.Tax));
        }

        [Test]
        public void Lease_LinksToAVehicleAndRoundTripsAsALease()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-lease-{Guid.NewGuid():N}.db");
            try
            {
                var assets = new SQLiteAssetDataStore(path);
                var accounts = new SQLiteAccountDataStore(path);
                var orchestrator = new AssetOrchestrator(
                    assets,
                    accounts,
                    new SQLiteAccountStatementDataStore(path),
                    new InMemoryTransactionDataStore(),
                    new InMemoryVehicleDataStore());

                var lease = new LeaseAccount
                {
                    Name = "Car lease",
                    Institution = "Ford",
                    AccountNumber = "3",
                    WebsiteUrl = "",
                    Type = AccountType.Lease,
                    Principal = -12000m,
                    InterestRate = 0.039m,
                    TermMonths = 36,
                    LeaseAmount = 18000m,
                    Residual = 15000m
                };
                accounts.UpsertAccount(lease);
                accounts.UpsertAccount(new MortgageAccount
                {
                    Name = "Mortgage",
                    Institution = "Bank",
                    AccountNumber = "9",
                    WebsiteUrl = "",
                    Type = AccountType.Mortgage
                });
                var car = new VehicleEv { Year = 2023, Make = "Ford", Model = "Mustang Mach-E", Vin = "LEASEVIN" };
                var vehicle = new VehicleAsset { Name = "Mach-E", Category = "Vehicle" };
                orchestrator.SaveAsset(vehicle, lease.Id, car);

                Assert.That(AccountKinds.Of(lease), Is.EqualTo(AccountKinds.Lease));
                Assert.That(StatementAccountMatch.ForAccount(lease), Is.EqualTo(StatementType.Loan));
                Assert.That(orchestrator.GetSecurableAccounts(vehicle).Single(), Is.TypeOf<LeaseAccount>());
                Assert.That(orchestrator.GetSecuredAccount(vehicle.Id), Is.TypeOf<LeaseAccount>());

                var house = new PropertyAsset { Name = "House" };
                orchestrator.SaveAsset(house, null, null);
                Assert.That(orchestrator.GetSecurableAccounts(house).Single(), Is.TypeOf<MortgageAccount>());

                var reloaded = new SQLiteAccountDataStore(path).GetAccount("Car lease");
                Assert.That(reloaded, Is.TypeOf<LeaseAccount>());
                var saved = (LeaseAccount)reloaded!;
                Assert.That(saved.Type, Is.EqualTo(AccountType.Lease));
                Assert.That(saved.Principal, Is.EqualTo(-12000m));
                Assert.That(saved.InterestRate, Is.EqualTo(0.039m).Within(0.0000001m));
                Assert.That(saved.TermMonths, Is.EqualTo(36));
                Assert.That(saved.LeaseAmount, Is.EqualTo(18000m));
                Assert.That(saved.Residual, Is.EqualTo(15000m));
                Assert.That(saved.SecuredAssetId, Is.EqualTo(vehicle.Id));
                Assert.That(LeaseEquity.IfKept(30000m, saved.Principal, saved.Residual), Is.EqualTo(3000m).Within(0.01m));
                Assert.That(AccountKinds.Of(saved), Is.EqualTo(AccountKinds.Lease));
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static AssetOrchestrator Orchestrator()
        {
            return new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                new InMemoryVehicleDataStore());
        }

        private static PropertyTaxStatement CrestingKnollsStatement(Guid collectorId) => new()
        {
            AccountId = collectorId,
            StatementDate = new DateTime(2026, 10, 26),
            DueDate = new DateTime(2027, 1, 31),
            TaxYear = 2026,
            LandMarketValue = 84500m,
            ImprovementMarketValue = 542600m,
            TotalMarketValue = 627100m,
            HomesteadCap = 0m,
            AssessedValue = 627100m,
            Exemptions = "Homestead, Veteran",
            Jurisdictions =
            [
                new() { Name = "Lamar CISD", ExemptionAmount = 145000m, TaxableValue = 482100m, RatePerHundred = 1.139m, TaxAmount = 5491.12m },
                new() { Name = "Harris F/B E", ExemptionAmount = 5000m, TaxableValue = 622100m, RatePerHundred = 0.092102m, TaxAmount = 572.97m },
                new() { Name = "Fort Bend C", ExemptionAmount = 130420m, TaxableValue = 496680m, RatePerHundred = 0.01m, TaxAmount = 49.67m },
                new() { Name = "Fort Bend Cc", ExemptionAmount = 130420m, TaxableValue = 496680m, RatePerHundred = 0.407m, TaxAmount = 2021.49m }
            ]
        };

        private static PropertyTaxStatement Bill(AssessmentMethod method)
        {
            var statement = CrestingKnollsStatement(Guid.NewGuid());
            statement.AssetId = Guid.NewGuid();
            statement.AmountDue = 8135.25m;
            statement.AssessmentMethod = method;
            statement.LevyMethod = LevyMethod.Manual;
            return statement;
        }

        private static void MatchLevy(PropertyTaxStatement statement)
        {
            foreach (var line in statement.Jurisdictions)
            {
                line.TaxableValue = PropertyLevy.TaxableValue(statement.AssessedValue, line.ExemptionAmount);
                line.TaxAmount = PropertyLevy.Tax(statement.LevyMethod, line.TaxableValue, line.RatePerHundred);
            }
        }

        private static PropertyTaxStatement TaxBill(Guid collectorId, DateTime process, DateTime due, decimal amount) => new()
        {
            AccountId = collectorId,
            AssetId = Guid.NewGuid(),
            StatementDate = process,
            DueDate = due,
            TaxYear = process.Year,
            AmountDue = amount,
            TotalMarketValue = amount,
            AssessedValue = amount
        };

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
    }
}
