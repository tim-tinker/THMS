using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Aggregation;
using THMS.Logic.Finance.Budget;
using THMS.Logic.Finance.Categories;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class CategoryRoleAndBudgetBalanceTests
    {
        [Test]
        public void Resolve_RootWithoutRole_IsExpense_AndChildInheritsUnlessOverridden()
        {
            var parent = Guid.NewGuid();
            var child = Guid.NewGuid();
            var overridden = Guid.NewGuid();
            var categories = new List<ExpenseCategory>
            {
                new() { Id = parent, Name = "Pay", Role = CategoryRole.Income },
                new() { Id = child, Name = "Bonus", ParentCategoryId = parent },
                new() { Id = overridden, Name = "Reimburse", ParentCategoryId = parent, Role = CategoryRole.Expense },
                new() { Id = DefaultExpenseCategories.GroceriesId, Name = "Groceries" }
            };

            Assert.That(CategoryRoleResolver.Resolve(DefaultExpenseCategories.GroceriesId, categories), Is.EqualTo(CategoryRole.Expense));
            Assert.That(CategoryRoleResolver.Resolve(child, categories), Is.EqualTo(CategoryRole.Income));
            Assert.That(CategoryRoleResolver.Resolve(overridden, categories), Is.EqualTo(CategoryRole.Expense));
        }

        [Test]
        public void PlanNet_NormalizesFrequency_AndLeavesOutMixedAndTransferRules()
        {
            var expenseId = Guid.NewGuid();
            var incomeId = Guid.NewGuid();
            var transferId = Guid.NewGuid();
            var categories = new List<ExpenseCategory>
            {
                new() { Id = expenseId, Name = "Food", Role = CategoryRole.Expense },
                new() { Id = incomeId, Name = "Paycheck", Role = CategoryRole.Income },
                new() { Id = transferId, Name = "Move", Role = CategoryRole.Transfer }
            };
            var food = Rule("Food", BudgetFrequency.Weekly, 12m, expenseId);
            var pay = Rule("Pay", BudgetFrequency.Monthly, 1000m, incomeId);
            var move = Rule("Move", BudgetFrequency.Monthly, 400m, transferId);
            var mixed = Rule("Mixed", BudgetFrequency.Monthly, 999m, expenseId, incomeId);
            var asOf = new DateTime(2026, 10, 21);

            var result = BudgetBalance.Compute(
                asOf,
                [food, pay, move, mixed],
                Periods(food, pay, move, mixed),
                categories,
                []);

            Assert.That(result.PlannedExpenses, Is.EqualTo(12m * 52m / 12m));
            Assert.That(result.PlannedIncome, Is.EqualTo(1000m));
            Assert.That(result.PlanNet, Is.EqualTo(1000m - (12m * 52m / 12m)));
            Assert.That(result.MixedRoleBudgets, Is.EqualTo(new[] { "Mixed" }));
            Assert.That(result.FullNet, Is.EqualTo(result.PlanNet));
        }

        [Test]
        public void FullNet_IgnoresTheOpenMonth_AndCoverageIsNotTheRecommendation()
        {
            var groceries = Guid.NewGuid();
            var categories = new List<ExpenseCategory>
            {
                new() { Id = groceries, Name = "Groceries", Role = CategoryRole.Expense, IsActive = true }
            };
            var asOf = new DateTime(2026, 10, 21);
            var activity = new List<CashContribution>();
            for (var month = new DateTime(2023, 11, 1); month < new DateTime(2026, 10, 1); month = month.AddMonths(1))
            {
                activity.Add(Spend(groceries, month.AddDays(4), 10m));
            }

            activity.Add(Spend(groceries, new DateTime(2026, 10, 4), 5000m));

            var result = BudgetBalance.Compute(asOf, [], new Dictionary<Guid, ExpenseBudgetHistory?>(), categories, activity);

            Assert.That(result.UnbudgetedExpenseRecommendation, Is.EqualTo(10m).Within(0.01m));
            Assert.That(result.FullNet, Is.EqualTo(-10m).Within(0.01m));
            Assert.That(result.CoverageTotal, Is.EqualTo(120m));
            Assert.That(result.CoverageMonthly, Is.EqualTo(10m));
        }

        [Test]
        public void FullNet_DoesNotInventUnbudgetedIncome()
        {
            var pay = Guid.NewGuid();
            var categories = new List<ExpenseCategory>
            {
                new() { Id = pay, Name = "Paycheck", Role = CategoryRole.Income, IsActive = true }
            };
            var activity = new List<CashContribution>
            {
                new()
                {
                    CategoryId = pay,
                    Date = new DateTime(2026, 8, 1),
                    Income = 3000m,
                    Amount = 3000m,
                    Role = CategoryRole.Income
                }
            };

            var result = BudgetBalance.Compute(
                new DateTime(2026, 10, 21),
                [],
                new Dictionary<Guid, ExpenseBudgetHistory?>(),
                categories,
                activity);

            Assert.That(result.PlannedIncome, Is.EqualTo(0m));
            Assert.That(result.FullNet, Is.EqualTo(0m));
            Assert.That(result.CoverageTotal, Is.EqualTo(0m));
        }

        [Test]
        public void InvestmentBudget_IsSetAside_NotAnExpense()
        {
            var id = Guid.NewGuid();
            var categories = new List<ExpenseCategory>
            {
                new() { Id = id, Name = "Investment Transfer", Role = CategoryRole.Investment, IsActive = true }
            };
            var rule = Rule("Invest", BudgetFrequency.Monthly, 500m, id);
            var result = BudgetBalance.Compute(
                new DateTime(2026, 10, 21),
                [rule],
                Periods(rule),
                categories,
                []);

            Assert.That(result.PlannedSetAside, Is.EqualTo(500m));
            Assert.That(result.PlannedExpenses, Is.EqualTo(0m));
            Assert.That(result.PlanNet, Is.EqualTo(-500m));
        }

        [Test]
        public void CashFlow_CountsBankDebtPaymentOnce_AndSplitsInterestByAccount()
        {
            var mortgageCategory = Guid.NewGuid();
            var charge = Guid.NewGuid();
            var earned = Guid.NewGuid();
            var invest = Guid.NewGuid();
            var transfer = Guid.NewGuid();
            var categories = new List<ExpenseCategory>
            {
                new() { Id = mortgageCategory, Name = "Mortgage", Role = CategoryRole.Expense },
                new() { Id = charge, Name = "Interest Charge", Role = CategoryRole.Expense },
                new() { Id = earned, Name = "Interest Income", Role = CategoryRole.Income },
                new() { Id = invest, Name = "Investment Transfer", Role = CategoryRole.Investment },
                new() { Id = transfer, Name = "Transfer", Role = CategoryRole.Transfer }
            };
            var bankId = Guid.NewGuid();
            var loanId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var investId = Guid.NewGuid();
            var accounts = new Dictionary<Guid, Account>
            {
                [bankId] = new BankAccount { Id = bankId, Name = "Checking" },
                [loanId] = new MortgageAccount { Id = loanId, Name = "House" },
                [cardId] = new CreditAccount { Id = cardId, Name = "Card" },
                [investId] = new InvestmentAccount { Id = investId, Name = "Brokerage" }
            };
            var posted = new List<PostedTransaction>
            {
                Tx(bankId, new DateTime(2026, 9, 2), -1800m, mortgageCategory),
                Tx(loanId, new DateTime(2026, 9, 2), 1700m, mortgageCategory),
                Tx(cardId, new DateTime(2026, 9, 3), -25m, charge),
                Tx(loanId, new DateTime(2026, 9, 3), -40m, charge),
                Tx(bankId, new DateTime(2026, 9, 4), 8m, earned),
                Tx(bankId, new DateTime(2026, 9, 5), -1000m, invest),
                Tx(investId, new DateTime(2026, 9, 5), 1000m, invest),
                Tx(bankId, new DateTime(2026, 9, 6), -300m, transfer)
            };
            var detector = new ExpenseBudgetDetector();
            var start = new DateTime(2026, 9, 1);
            var end = new DateTime(2026, 9, 30);

            Assert.That(detector.ComputeActualExpenses(posted, [mortgageCategory], start, end, categories, accounts), Is.EqualTo(1800m));
            Assert.That(detector.ComputeActualExpenses(posted, [charge], start, end, categories, accounts), Is.EqualTo(25m));
            Assert.That(detector.ComputeActualExpenses(posted, [earned], start, end, categories, accounts), Is.EqualTo(8m));
            Assert.That(detector.ComputeActualExpenses(posted, [invest], start, end, categories, accounts), Is.EqualTo(1000m));
            Assert.That(detector.ComputeActualExpenses(posted, [transfer], start, end, categories, accounts), Is.EqualTo(0m));

            var snapshot = new FinanceDashboardComposer().Compose(
                new DateTime(2026, 9, 15),
                accounts.Values.ToList(),
                posted,
                [],
                [],
                new Dictionary<Guid, ExpenseBudgetHistory?>(),
                [],
                [],
                categories);
            var september = snapshot.MonthlyTrend.Single(point => point.Month == start);
            var rows = snapshot.Activity.Where(row => row.Month == start).ToList();
            Assert.That(september.Spending, Is.EqualTo(1825m));
            Assert.That(september.Income, Is.EqualTo(8m));
            Assert.That(rows.Sum(row => row.Spending), Is.EqualTo(september.Spending));
            Assert.That(rows.Sum(row => row.Income), Is.EqualTo(september.Income));
            Assert.That(rows.Sum(row => row.SetAside), Is.EqualTo(1000m));
            var slice = snapshot.CategorySlices.Single(s => s.CategoryId == mortgageCategory);
            Assert.That(slice.Amount, Is.EqualTo(1800m));
            Assert.That(rows.Where(row => row.CategoryId == mortgageCategory).Sum(row => row.Spending), Is.EqualTo(slice.Amount));
        }

        [Test]
        public void Sqlite_StoresCategoryRoleWithoutRewritingNames()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-role-{Guid.NewGuid():N}.db");
            try
            {
                var id = Guid.NewGuid();
                var store = new SQLiteTransactionDataStore(path);
                store.AddCategory(new ExpenseCategory
                {
                    Id = id,
                    Name = "Interest Income",
                    IsActive = true,
                    DisplayOrder = 20,
                    Role = CategoryRole.Income
                });

                var again = new SQLiteTransactionDataStore(path);
                var loaded = again.GetCategory(id);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.Name, Is.EqualTo("Interest Income"));
                Assert.That(loaded.Role, Is.EqualTo(CategoryRole.Income));
                Assert.That(again.GetCategory(DefaultExpenseCategories.GroceriesId)!.Role, Is.Null);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                {
                    var file = path + suffix;
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        private static CashContribution Spend(Guid categoryId, DateTime date, decimal amount) =>
            new()
            {
                CategoryId = categoryId,
                Date = date,
                Amount = -amount,
                Spending = amount,
                Role = CategoryRole.Expense
            };

        private static PostedTransaction Tx(Guid accountId, DateTime date, decimal amount, Guid categoryId) =>
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Date = date,
                Amount = amount,
                CategoryId = categoryId
            };

        private static ExpenseBudgetRule Rule(string name, BudgetFrequency frequency, decimal amount, params Guid[] categories) =>
            new()
            {
                Id = Guid.NewGuid(),
                BudgetName = name,
                BudgetFrequency = frequency,
                DefaultBudgetAmount = amount,
                IncludedCategoryIds = categories.ToList(),
                IsActive = true
            };

        private static Dictionary<Guid, ExpenseBudgetHistory?> Periods(params ExpenseBudgetRule[] rules) =>
            rules.ToDictionary(
                rule => rule.Id,
                rule => (ExpenseBudgetHistory?)new ExpenseBudgetHistory
                {
                    BudgetRuleId = rule.Id,
                    BudgetAmount = rule.DefaultBudgetAmount,
                    PeriodStart = new DateTime(2026, 10, 1),
                    PeriodEnd = new DateTime(2026, 10, 31)
                });
    }
}
