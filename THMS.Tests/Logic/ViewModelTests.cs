using System.ComponentModel;
using System.Reflection;
using THMS.Data.Stores;
using THMS.Domain;
using THMS.Domain.Energy;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Domain.Transportation;
using THMS.Ingestion;
using THMS.Logic.Energy;
using THMS.Logic.Finance.Aggregation;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Energy;
using THMS.Logic.ViewModels.Finance;
using THMS.Logic.ViewModels.Transportation;
using THMS.Tests.Logic.TestSupport;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class SimpleViewModelTests
    {
        [Test]
        public void HomeHouseholdSettingsAndBaseDashboard()
        {
            Assert.That(new HomeViewModel().WelcomeMessage, Does.Contain("financial overview"));

            var household = new HouseholdDashboardViewModel();
            household.Initialize();
            household.Activate();
            household.Deactivate();
            household.Dispose();
            household.Expenses.Add(new HouseholdExpense { Category = "Food" });
            household.SelectedExpense = household.Expenses[0];
            Assert.That(household.SelectedExpense, Is.Not.Null);

            var settings = new SettingsDashboardViewModel
            {
                DarkModeEnabled = true,
                AutoSaveEnabled = false,
                ShowTooltips = false
            };
            Assert.That(settings.DarkModeEnabled, Is.True);
            Assert.That(settings.AutoSaveEnabled, Is.False);
        }
    }

    [TestFixture]
    public class EnergyViewModelTests
    {
        [Test]
        public void SummaryPeriodAndRecordViewModels_CopyDomain()
        {
            var summary = new EnergySummaryViewModel(new EnergySummary
            {
                ProducedKwh = 10,
                ConsumedKwh = 5,
                GridImportKwh = 2,
                GridExportKwh = 1,
                BatteryChargeKwh = 3,
                BatteryDischargeKwh = 4,
                EvChargeKwh = 1
            });
            Assert.That(summary.NetImportKwh, Is.EqualTo(1));
            Assert.That(summary.EvConsumption, Is.EqualTo(0.2m));

            var record = new EnergyPeriodRecord
            {
                Date = new DateTime(2026, 1, 1),
                Intervals =
                [
                    new EnergyIntervalRecord { Timestamp = new DateTime(2026, 1, 1, 1, 0, 0), SolarKwh = 1 }
                ],
                SolarKwh = 2,
                HomeConsumptionKwh = 3,
                GridImportKwh = 4,
                GridExportKwh = 5,
                BatteryChargeKwh = 6,
                BatteryDischargeKwh = 7,
                EvChargeKwh = 8,
                HourlyMin = 1,
                HourlyMax = 2,
                HourlyAvg = 1.5m,
                HourlyStdDev = 0.1m,
                TimeOfMax = new DateTime(2026, 1, 1, 12, 0, 0),
                PeakWindowStart = TimeSpan.FromHours(1),
                PeakWindowEnd = TimeSpan.FromHours(3),
                SolarPeakKw = 9,
                SolarPeakTime = new DateTime(2026, 1, 1, 13, 0, 0),
                DailyBatteryChargeDuration = TimeSpan.FromMinutes(30),
                DailyBatteryDischargeDuration = TimeSpan.FromMinutes(15),
                EvSolarKwh = 1,
                EvGridKwh = 2,
                EvBatteryKwh = 3
            };
            var recordVm = new EnergyPeriodRecordViewModel(record);
            Assert.That(recordVm.SolarKwh, Is.EqualTo(2));
            Assert.That(recordVm.EvBatteryKwh, Is.EqualTo(3));

            var emptyPeriod = new EnergyPeriodViewModel();
            Assert.That(emptyPeriod.GetDailyStats(r => r.SolarKwh), Is.EqualTo((0m, 0m, 0m, 0m)));
            Assert.That(emptyPeriod.Intervals, Is.Empty);

            var period = new EnergyPeriod
            {
                Start = new DateTime(2026, 1, 1),
                End = new DateTime(2026, 1, 2),
                Records = [record],
                SolarKwh = 2,
                HomeConsumptionKwh = 3,
                GridImportKwh = 4,
                GridExportKwh = 5,
                BatteryChargeKwh = 6,
                BatteryDischargeKwh = 7,
                EvChargeKwh = 8,
                PeriodMin = 1,
                PeriodMax = 2,
                PeriodAvg = 1.5m,
                PeriodStdDev = 0.1m,
                PeriodTimeOfMax = record.TimeOfMax,
                PeriodPeakWindowStart = record.PeakWindowStart,
                PeriodPeakWindowEnd = record.PeakWindowEnd,
                PeriodSolarPeakKw = 9,
                PeriodSolarPeakTime = record.SolarPeakTime,
                BatteryContributionToHomeLoad = 0.2m,
                EvSolarKwhTotal = 1,
                EvGridKwhTotal = 2,
                EvBatteryKwhTotal = 3
            };
            var periodVm = new EnergyPeriodViewModel(period);
            Assert.That(periodVm.Intervals, Has.Count.EqualTo(1));
            var stats = periodVm.GetDailyStats(r => r.SolarKwh);
            Assert.That(stats.Min, Is.EqualTo(2));
            Assert.That(stats.Max, Is.EqualTo(2));
        }

        [Test]
        public void EnergyDashboardViewModel_NavigatesTabsAndDates()
        {
            var service = new EnergyAggregationService(new InMemoryEnergyDataStore());
            var vm = new EnergyDashboardViewModel(service);

            foreach (EnergyTab tab in Enum.GetValues<EnergyTab>())
            {
                vm.SelectedTab = tab;
                vm.Refresh();
                Assert.That(vm.Summary, Is.Not.Null);
                Assert.That(vm.Custom.Records, Is.Empty);
            }

            vm.SelectedTab = (EnergyTab)99;
            vm.Refresh();

            vm.MoveDay(1);
            vm.JumpToDay(new DateTime(2026, 1, 1));
            vm.MoveWeek(-1);
            vm.JumpToWeek(new DateTime(2026, 2, 1));
            vm.MoveMonth(1);
            vm.JumpToMonth(new DateTime(2026, 3, 1));
            vm.MoveYear(-1);
            vm.JumpToYear(new DateTime(2025, 1, 1));
            Assert.That(vm.Day, Is.Not.Null);
        }
    }

    [TestFixture]
    public class FinanceViewModelTests
    {
        [Test]
        public void UnifiedAccountViewBuilder_MapsAllTypes()
        {
            var views = UnifiedAccountViewBuilder.Build(
            [
                new BankAccount { Name = "B", Institution = "I", AccountNumber = "1", PostedBalance = 10, OverdraftLimit = 2, BalanceAsOf = DateTime.Today, WebsiteUrl = "https://bank.example" },
                new CreditAccount { Name = "C", PostedBalance = 3, CreditLimit = 4, DueDate = DateTime.Today },
                new InvestmentAccount { Name = "Inv", CashBalance = 5 },
                new LoanAccount { Name = "L", Principal = 7, InterestRate = 0.05m },
                new MortgageAccount { Name = "M", Principal = 8, InterestRate = 0.04m, NextPaymentDate = DateTime.Today },
                new InternalAccount { Name = "Int" },
                new UntrackedAccount { Name = "Power", Type = AccountType.Utility },
                new UntrackedAccount { Name = "Lawn", Type = AccountType.Service },
                new UntrackedAccount { Name = "Home", Type = AccountType.Insurance }
            ]);

            Assert.That(views, Has.Count.EqualTo(9));
            Assert.That(views[0].AccountType, Is.EqualTo("Bank"));
            Assert.That(views[1].AccountType, Is.EqualTo("Credit"));
            Assert.That(views[2].AccountType, Is.EqualTo("Investment"));
            Assert.That(views[3].AccountType, Is.EqualTo("Loan"));
            Assert.That(views[4].AccountType, Is.EqualTo("Mortgage"));
            Assert.That(views[5].AccountType, Is.EqualTo("Internal"));
            Assert.That(views[6].AccountType, Is.EqualTo("Utility"));
            Assert.That(views[7].AccountType, Is.EqualTo("Service"));
            Assert.That(views[8].AccountType, Is.EqualTo("Insurance"));
            Assert.That(views[0].BankCreditAvailable, Is.EqualTo(2));
            Assert.That(views[1].CreditLimit, Is.EqualTo(4));
            Assert.That(views[1].Balance, Is.EqualTo(-3));
            Assert.That(views[1].BankCreditAvailable, Is.EqualTo(7));
            Assert.That(views[2].Balance, Is.EqualTo(5));
            Assert.That(views[3].APR, Is.EqualTo(0.05m));
            Assert.That(views[4].APR, Is.EqualTo(0.04m));
            Assert.That(views[5].Balance, Is.Null);
            Assert.That(views[0].WebsiteUrl, Is.EqualTo("https://bank.example"));
            Assert.That(views[1].WebsiteUrl, Is.EqualTo(""));
            Assert.That(views[1].DueDate, Is.EqualTo(DateTime.Today));
            Assert.That(views[3].DueDate, Is.Null);
            Assert.That(views[4].DueDate, Is.Null);
        }

        [Test]
        public void UnifiedAccountViewBuilder_UsesRecurringRulesForLoanAndMortgageDueDates()
        {
            var loanId = Guid.NewGuid();
            var mortgageId = Guid.NewGuid();
            var next = new DateTime(2026, 10, 15);
            var views = UnifiedAccountViewBuilder.Build(
                [
                    new LoanAccount { Id = loanId, Name = "L", Principal = 7 },
                    new MortgageAccount { Id = mortgageId, Name = "M", Principal = 8, NextPaymentDate = DateTime.Today }
                ],
                new Dictionary<Guid, DateTime?>
                {
                    [loanId] = next,
                    [mortgageId] = next.AddDays(5)
                });

            Assert.That(views[0].DueDate, Is.EqualTo(next));
            Assert.That(views[1].DueDate, Is.EqualTo(next.AddDays(5)));
        }

        [Test]
        public void UnifiedAccountViewBuilder_CreditBalanceIsAmountOwed()
        {
            var owed = UnifiedAccountViewBuilder.Build(
                [new CreditAccount { Name = "C", PostedBalance = -1500, CreditLimit = 5000 }]);
            Assert.That(owed[0].Balance, Is.EqualTo(1500m));
            Assert.That(owed[0].BankCreditAvailable, Is.EqualTo(3500m));

            var creditBalance = UnifiedAccountViewBuilder.Build(
                [new CreditAccount { Name = "C", PostedBalance = 200, CreditLimit = 5000 }]);
            Assert.That(creditBalance[0].Balance, Is.EqualTo(-200m));
            Assert.That(creditBalance[0].BankCreditAvailable, Is.EqualTo(5200m));
        }

        [Test]
        public void UnifiedAccountViewBuilder_OmitsPostedBalanceWhenNotUsable()
        {
            var bankId = Guid.NewGuid();
            var creditId = Guid.NewGuid();
            var loanId = Guid.NewGuid();
            var usable = new HashSet<Guid> { bankId };
            var views = UnifiedAccountViewBuilder.Build(
                [
                    new BankAccount { Id = bankId, Name = "B", PostedBalance = 10, OverdraftLimit = 2 },
                    new CreditAccount { Id = creditId, Name = "C", PostedBalance = -1500, CreditLimit = 5000 },
                    new LoanAccount { Id = loanId, Name = "L", Principal = 7 }
                ],
                usablePostedBalanceAccountIds: usable);

            Assert.That(views[0].Balance, Is.EqualTo(10m));
            Assert.That(views[1].Balance, Is.Null);
            Assert.That(views[1].BankCreditAvailable, Is.Null);
            Assert.That(views[1].CreditLimit, Is.EqualTo(5000m));
            Assert.That(views[2].Balance, Is.Null);
        }

        [Test]
        public void UnifiedTransactionViewBuilder_IncludesPostedAndUserFuturesAndSorts()
        {
            var account = Guid.NewGuid();
            var views = UnifiedTransactionViewBuilder.Build(
                [new PostedTransaction { AccountId = account, Date = new DateTime(2026, 1, 5), Description = null, Amount = 1 }],
                [new PostedTransferTransaction { AccountId = account, Date = new DateTime(2026, 1, 4), Description = "T", Amount = 2 }],
                [
                    new FutureSingleTransaction { AccountId = account, Date = new DateTime(2026, 1, 3), Amount = 3, IsUserCreated = true },
                    new FutureSingleTransaction { AccountId = account, Date = new DateTime(2026, 1, 3).AddHours(1), Amount = 3.5m, IsUserCreated = false }
                ],
                [
                    new FutureTransferTransaction { FromAccountId = account, Date = new DateTime(2026, 1, 2), Amount = 4, IsUserCreated = true },
                    new FutureTransferTransaction { FromAccountId = account, Date = new DateTime(2026, 1, 2).AddHours(1), Amount = 4.5m, IsUserCreated = false }
                ]);

            Assert.That(views, Has.Count.EqualTo(4));
            Assert.That(views.First().Type, Is.EqualTo(UnifiedTransactionView.FutureTransferType));
            Assert.That(views.Last().Type, Is.EqualTo(UnifiedTransactionView.PostedType));
            Assert.That(views.Any(v => v.Type == UnifiedTransactionView.FutureType), Is.True);
            Assert.That(views.First(v => v.Type == UnifiedTransactionView.PostedType).Description, Is.EqualTo(""));
            Assert.That(views.All(v => v.Type is not "RecurringRule" and not "RecurringTransferRule"), Is.True);
        }

        [Test]
        public void UnifiedTransactionViewBuilder_BuildRecurringRules()
        {
            var account = Guid.NewGuid();
            var other = Guid.NewGuid();
            var views = UnifiedTransactionViewBuilder.BuildRecurringRules(
                [
                    new RecurringSingleTransactionRule
                    {
                        AccountId = account,
                        Description = "Rent",
                        Amount = -10,
                        Category = "Housing",
                        NextOccurrence = new DateTime(2026, 2, 1)
                    }
                ],
                [
                    new RecurringTransferRule
                    {
                        FromAccountId = account,
                        ToAccountId = other,
                        Description = "Sweep",
                        Amount = 20,
                        NextOccurrence = new DateTime(2026, 1, 15)
                    }
                ]);

            Assert.That(views, Has.Count.EqualTo(2));
            Assert.That(views.Single(v => v.Type == UnifiedTransactionView.RecurringRuleType).Description, Is.EqualTo("Rent"));
            Assert.That(views.Single(v => v.Type == UnifiedTransactionView.RecurringTransferRuleType).Description, Is.EqualTo("Sweep"));
            Assert.That(views.All(v => v.IsRecurringRule), Is.True);
            Assert.That(views.All(v => !v.IsForecasted), Is.True);
        }

        [Test]
        public void UnifiedTransactionView_DisplayAndRunningBalanceOrdersAreReversesWithAmount()
        {
            var day = new DateTime(2026, 1, 5);
            var earlierId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var laterId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var items = new List<UnifiedTransactionView>
            {
                new() { Id = earlierId, Date = day, Amount = 1, Type = "Posted" },
                new() { Id = laterId, Date = day, Amount = 10, Type = "Posted" },
                new() { Id = Guid.NewGuid(), Date = day.AddDays(-1), Amount = 5, Type = "Posted" }
            };

            var display = UnifiedTransactionView.OrderForDisplay(items).Select(t => (t.Date, t.Amount, t.Id)).ToList();
            var running = UnifiedTransactionView.OrderForRunningBalance(items).Select(t => (t.Date, t.Amount, t.Id)).ToList();

            Assert.That(display, Is.EqualTo(new[]
            {
                (day, 1m, earlierId),
                (day, 10m, laterId),
                (day.AddDays(-1), 5m, items[2].Id)
            }));
            Assert.That(running, Is.EqualTo(display.AsEnumerable().Reverse().ToList()));
        }

        [Test]
        public void FinanceDashboardViewModel_Refresh_BuildsHouseholdSnapshot()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var checking = new BankAccount
            {
                Id = Guid.NewGuid(),
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                PostedBalance = 1200
            };
            var card = new CreditAccount
            {
                Id = Guid.NewGuid(),
                Name = "Visa",
                Institution = "Bank",
                AccountNumber = "2",
                PostedBalance = -400,
                CreditLimit = 1000
            };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(card);
            accounts.UpsertAccount(new InternalAccount
            {
                Id = Guid.NewGuid(),
                Name = "Internal",
                Institution = "House",
                AccountNumber = "3"
            });

            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today,
                Amount = -25,
                CategoryId = DefaultExpenseCategories.UncategorizedId,
                Category = DefaultExpenseCategories.Uncategorized
            });
            txs.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Rent",
                Amount = -900,
                NextOccurrence = DateTime.Today.AddDays(3),
                IsActive = true
            });

            var vm = new FinanceDashboardViewModel(accounts, txs);
            vm.Initialize();

            Assert.That(vm.Snapshot.BankBalance, Is.EqualTo(1200m));
            Assert.That(vm.Snapshot.CreditOwed, Is.EqualTo(400m));
            Assert.That(vm.Snapshot.NetLiquid, Is.EqualTo(800m));
            Assert.That(vm.Snapshot.UncategorizedCount, Is.EqualTo(1));
            Assert.That(vm.Snapshot.Alerts.Any(a => a.Contains("High utilization")), Is.True);
            Assert.That(vm.Snapshot.UpcomingPayments.Any(p => p.Description == "Rent"), Is.True);
            Assert.That(vm.Snapshot.UpcomingForecast.Any(f => f.Description == "Rent"), Is.True);
        }
    }

    [TestFixture]
    public class FinanceDashboardComposerTests
    {
        [Test]
        public void Compose_NetsReimbursementsAndFlagsOverspentBudget()
        {
            var asOf = new DateTime(2026, 9, 7);
            var accountId = Guid.NewGuid();
            var ruleId = Guid.NewGuid();
            var checking = new BankAccount { Id = accountId, Name = "Checking", PostedBalance = 50 };
            var period = new ExpenseBudgetHistory
            {
                BudgetRuleId = ruleId,
                PeriodStart = new DateTime(2026, 9, 1),
                PeriodEnd = new DateTime(2026, 9, 10),
                StartingBalance = 0,
                BudgetAmount = 40,
                ActualExpenses = 55,
                RecommendedAmount = 45
            };
            period.RecalculateRemaining();

            var snapshot = new FinanceDashboardComposer().Compose(
                asOf,
                [checking],
                [
                    new PostedTransaction
                    {
                        AccountId = accountId,
                        Date = new DateTime(2026, 9, 2),
                        Amount = 10,
                        CategoryId = DefaultExpenseCategories.GroceriesId,
                        Category = DefaultExpenseCategories.Groceries
                    },
                    new PostedTransaction
                    {
                        AccountId = accountId,
                        Date = new DateTime(2026, 9, 3),
                        Amount = -50,
                        CategoryId = DefaultExpenseCategories.GroceriesId,
                        Category = DefaultExpenseCategories.Groceries
                    },
                    new PostedTransaction
                    {
                        AccountId = accountId,
                        Date = new DateTime(2026, 9, 4),
                        Amount = 2000,
                        CategoryId = DefaultExpenseCategories.PaymentId,
                        Category = DefaultExpenseCategories.Payment
                    }
                ],
                [],
                [
                    new ExpenseBudgetRule
                    {
                        Id = ruleId,
                        AccountId = accountId,
                        BudgetName = "Groceries",
                        IncludedCategoryIds = [DefaultExpenseCategories.GroceriesId],
                        BudgetFrequency = BudgetFrequency.Monthly,
                        IsActive = true
                    }
                ],
                new Dictionary<Guid, ExpenseBudgetHistory?> { [ruleId] = period },
                [],
                [],
                DefaultExpenseCategories.All.ToList());

            Assert.That(snapshot.Budgets.Single().Status, Is.EqualTo("Overspent, Ends soon"));
            Assert.That(snapshot.CategorySlices.Single(s => s.Name == DefaultExpenseCategories.Groceries).Amount, Is.EqualTo(40m));
            Assert.That(snapshot.MonthlyTrend.Single(p => p.Month == new DateTime(2026, 9, 1)).Spending, Is.EqualTo(40m));
            Assert.That(snapshot.MonthlyTrend.Single(p => p.Month == new DateTime(2026, 9, 1)).Income, Is.EqualTo(2000m));
            Assert.That(snapshot.Alerts.Any(a => a.StartsWith("Overspent: Groceries")), Is.True);
        }

        [Test]
        public void CreditOwed_UsesNegativePostedBalance()
        {
            var credit = new CreditAccount { PostedBalance = -250, CreditLimit = 1000 };
            Assert.That(FinanceDashboardComposer.CreditOwed(credit), Is.EqualTo(250m));
            Assert.That(FinanceDashboardComposer.Utilization(credit), Is.EqualTo(0.25m));
            Assert.That(FinanceDashboardComposer.CreditOwed(new CreditAccount { PostedBalance = 50, CreditLimit = 1000 }), Is.EqualTo(0m));
        }
    }

    [TestFixture]
    public class VehicleAndTransportationViewModelTests
    {
        [Test]
        public void VehicleListItemViewModel_ApplySummaryAndNotifications()
        {
            var item = new VehicleListItemViewModel(Guid.NewGuid(), "Car", true);
            var changed = new List<string>();
            item.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

            item.ApplySummary(new EvTransportationCostSummary { CostPerMile = 0.2m, TotalMiles = 10, TotalCost = 2 });
            item.ApplySummary(new IceTransportationCostSummary { CostPerMile = 0.3m, TotalMiles = 20, TotalCost = 6 });
            item.ApplySummary("nope");

            Assert.That(item.IsEv, Is.True);
            Assert.That(item.CostPerMile, Is.EqualTo(0.3m));
            Assert.That(item.TotalMiles, Is.EqualTo(20));
            Assert.That(item.TotalCost, Is.EqualTo(6));
            Assert.That(changed, Does.Contain("CostPerMile"));
        }

        [Test]
        public void VehicleListViewModel_LoadsAddsAndActivates()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var finance = new InMemoryFinanceDataStore();
            var vm = new VehicleListViewModel(vehicles, finance);
            vm.Initialize();
            Assert.That(vm.Vehicles, Has.Count.EqualTo(2));
            vm.Activate();

            var extra = new VehicleIce { Id = Guid.NewGuid(), Name = "Spare" };
            vm.AddVehicle(extra);
            Assert.That(vm.Vehicles.Any(v => v.VehicleId == extra.Id), Is.True);
            Assert.That(vm.HistoryPeriod, Is.EqualTo("Month"));
            Assert.That(vm.PeriodStart, Is.EqualTo(DateTime.Today.AddMonths(-1)));

            vm.HistoryPeriod = "Year";
            Assert.That(vm.PeriodStart, Is.EqualTo(DateTime.Today.AddYears(-1)));
            vm.HistoryPeriod = "Lifetime";
            Assert.That(vm.PeriodStart, Is.EqualTo(DateTime.MinValue));

            var noMileage = new VehicleDetailViewModel(Guid.NewGuid(), new InMemoryVehicleDataStore());
            Assert.That(noMileage.HistoryPeriod, Is.EqualTo("Month"));
            Assert.That(noMileage.StartTime, Is.EqualTo(DateTime.Today.AddMonths(-1)));
        }

        [Test]
        public void VehicleDetailViewModel_RefreshFilterAndSessionUpsert()
        {
            var store = new InMemoryVehicleDataStore();
            var ev = store.GetAllVehicles().OfType<VehicleEv>().First();
            store.UpsertIceMileageRecord(new IceMileageRecord
            {
                VehicleId = ev.Id,
                EndTime = new DateTime(2026, 1, 1),
                OdometerMiles = 10
            });
            var session = new CommercialEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2026, 2, 1),
                EndTime = new DateTime(2026, 2, 1, 1, 0, 0),
                OdometerMiles = 20
            };
            store.UpsertBaseEvChargeSession(session);
            store.UpsertMaintenanceInvoice(new MaintenanceInvoiceRecord
            {
                VehicleId = ev.Id,
                Date = new DateTime(2026, 2, 2),
                Cost = 50
            });

            var vm = new VehicleDetailViewModel(ev.Id, store);
            Assert.That(vm.HistoryPeriod, Is.EqualTo("Month"));
            Assert.That(vm.GetLatestChargeSession(), Is.Not.Null);

            vm.HistoryPeriod = "Lifetime";
            Assert.That(vm.StartTime, Is.EqualTo(DateTime.MinValue));
            Assert.That(vm.ChargeCostRows, Has.Count.EqualTo(1));
            Assert.That(vm.ChargeCostRows[0].MilesDriven, Is.EqualTo(20));
            Assert.That(vm.ChargeCostRows[0].Cost, Is.EqualTo(0));

            var changed = false;
            vm.PropertyChanged += (_, _) => changed = true;
            vm.StartTime = vm.StartTime;
            Assert.That(changed, Is.False);
            vm.StartTime = new DateTime(2026, 1, 15);
            vm.EndTime = vm.EndTime;
            vm.EndTime = new DateTime(2026, 12, 31);
            Assert.That(changed, Is.True);

            var inRange = new CommercialEvChargeSession
            {
                Id = Guid.NewGuid(),
                VehicleId = ev.Id,
                StartTime = new DateTime(2026, 6, 1)
            };
            vm.UpsertChargeSession(inRange);
            vm.UpsertChargeSession(inRange);
            inRange.StartTime = new DateTime(1999, 1, 1);
            vm.UpsertChargeSession(inRange);
            vm.UpsertChargeSession(new CommercialEvChargeSession
            {
                Id = Guid.NewGuid(),
                StartTime = new DateTime(1990, 1, 1)
            });
            Assert.That(vm.ChargeSessions.Any(s => s.Id == inRange.Id), Is.False);
        }

        [Test]
        public void VehicleChargeCostRow_FromSession_ComputesMilesAndCostPerMile()
        {
            var row = VehicleChargeCostRow.FromSession(new CommercialEvChargeSession
            {
                StartTime = new DateTime(2026, 2, 1, 12, 0, 0),
                LastOdometer = 100,
                OdometerMiles = 140,
                SessionCost = 10
            });

            Assert.That(row.StartTime, Is.EqualTo(new DateTime(2026, 2, 1, 12, 0, 0)));
            Assert.That(row.MilesDriven, Is.EqualTo(40));
            Assert.That(row.Cost, Is.EqualTo(10));
            Assert.That(row.CostPerMile, Is.EqualTo(0.25m));

            var noMiles = VehicleChargeCostRow.FromSession(new CommercialEvChargeSession
            {
                LastOdometer = 200,
                OdometerMiles = 150,
                SessionCost = 8
            });
            Assert.That(noMiles.MilesDriven, Is.EqualTo(0));
            Assert.That(noMiles.CostPerMile, Is.EqualTo(0));
        }

        [Test]
        public void EvChargeSessionViewModel_DerivedMetrics()
        {
            var vehicle = new VehicleEv { BatteryCapacityKwh = 100 };
            Assert.That(() => new EvChargeSessionViewModel(vehicle, null), Throws.ArgumentNullException);

            var session = new HomeEvChargeSession
            {
                LastOdometer = 100,
                OdometerMiles = 90,
                LastSoc = 40,
                StartSoc = 50,
                EndSoc = 40
            };
            var empty = new EvChargeSessionViewModel(vehicle, session);
            Assert.That(empty.MilesUsed, Is.EqualTo(0));
            Assert.That(empty.SocUsed, Is.EqualTo(0));
            Assert.That(empty.SocAdded, Is.EqualTo(0));
            Assert.That(empty.Mpge, Is.EqualTo(0));

            empty.LastOdometer = 100;
            empty.Odometer = 120;
            empty.LastSoc = 80;
            empty.StartSoc = 50;
            empty.EndSoc = 90;
            empty.StartTime = DateTime.Today;
            empty.EndTime = DateTime.Today.AddHours(1);
            Assert.That(empty.MilesUsed, Is.EqualTo(20));
            Assert.That(empty.KwhUsed, Is.EqualTo(30));
            Assert.That(empty.WhPerMile, Is.EqualTo(1500));
            Assert.That(empty.Mpge, Is.GreaterThan(0));
            Assert.That(empty.SessionId, Is.EqualTo(session.Id));

            _ = new EvChargeSessionViewModel(vehicle, new CommercialEvChargeSession());
        }

        [Test]
        public void MileageAndChargeEntry_SavesAgainstVehicleStore()
        {
            var store = new InMemoryVehicleDataStore();
            var ice = store.GetAllVehicles().OfType<VehicleIce>().First();
            var mileage = new MileageEntryViewModel(ice.Id, store)
            {
                Date = new DateTime(2026, 1, 1),
                OdometerMiles = 123,
                GallonsAdded = 8,
                FuelCost = 30,
                IsFullFillUp = true
            };
            mileage.Save();
            Assert.That(store.GetIceMileageRecords(ice.Id, DateTime.MinValue, DateTime.MaxValue).Any(), Is.True);

            var charge = new ChargeCostEntryViewModel(ice.Id, store)
            {
                Timestamp = DateTime.Today,
                Cost = 4
            };
            charge.Save();
            Assert.That(charge.VehicleId, Is.EqualTo(ice.Id));
        }
    }

    [TestFixture]
    public class ImporterViewModelTests
    {
        [Test]
        public void Import_CoversValidationSuccessErrorAndUnknownType()
        {
            var pipeline = new EnergyIngestionPipeline(
                new InMemoryEnergyDataStore(),
                new InMemoryFinanceDataStore(),
                new InMemoryVehicleDataStore());
            var vm = new ImporterViewModel(pipeline);
            var names = new List<string>();
            vm.PropertyChanged += (_, e) => names.Add(e.PropertyName!);

            vm.Import();
            Assert.That(vm.StatusMessage, Is.EqualTo("No file selected."));

            vm.SelectedFile = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.csv");
            vm.SelectedImporter = ImporterType.ChargePoint;
            vm.Import();
            Assert.That(vm.StatusMessage, Does.StartWith("Error:"));
            Assert.That(vm.IsBusy, Is.False);

            var circuit = Path.Combine(Path.GetTempPath(), $"circuit-{Guid.NewGuid():N}.csv");
            File.WriteAllText(circuit,
                "Local SPAN Panel time (America/Chicago),Energy Data (Wh)\n2026-01-01 00:00:00,1\n");
            vm.SelectedFile = circuit;
            vm.SelectedImporter = ImporterType.HomeCircuit;
            vm.Import();
            Assert.That(vm.StatusMessage, Is.EqualTo("Import complete."));

            var solar = Path.Combine(Path.GetTempPath(), $"solar-{Guid.NewGuid():N}.csv");
            File.WriteAllText(solar,
                "Date/Time,Energy Produced (Wh),Energy Consumed (Wh),Exported to Grid (Wh),Imported from Grid (Wh),Stored in batteries (Wh),Discharged from batteries (Wh)\n" +
                "2026-01-01 00:00:00,1,1,0,0,0,0\n");
            vm.SelectedFile = solar;
            vm.SelectedImporter = ImporterType.SolarProduction;
            vm.Import();
            Assert.That(vm.StatusMessage, Is.EqualTo("Import complete."));

            var chargeCsv = Path.Combine(Path.GetTempPath(), $"cp-{Guid.NewGuid():N}.csv");
            File.WriteAllText(chargeCsv,
                "Start,End,Duration,Energy (kWh),Cost,Station\n" +
                "\"7/8/2026, 5:15 PM CDT\",\"7/8/2026, 7:57 PM CDT\",2h 42m 25s,10.5,4.00,Station A\n");
            vm.SelectedFile = chargeCsv;
            vm.SelectedImporter = ImporterType.ChargePoint;
            vm.Import();
            Assert.That(vm.StatusMessage, Is.EqualTo("Import complete."));
            Assert.That(vm.IsBusy, Is.False);

            vm.SelectedImporter = (ImporterType)123;
            vm.Import();
            Assert.That(vm.StatusMessage, Does.Contain("Unknown importer type"));
            Assert.That(names, Does.Contain(nameof(ImporterViewModel.SelectedFile)));
        }
    }
}
