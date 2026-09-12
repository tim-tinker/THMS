using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Budget;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Transactions;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class SplitTransactionTests
    {
        [Test]
        public void Validator_RequiresMatchingSumAndTransferAccount()
        {
            var parent = Guid.NewGuid();
            var splits = new List<SplitTransactionRow>
            {
                new() { Amount = -40, Type = SplitType.Expense, CategoryId = DefaultExpenseCategories.GroceriesId, Category = DefaultExpenseCategories.Groceries },
                new() { Amount = -10, Type = SplitType.Expense, CategoryId = DefaultExpenseCategories.RestaurantsId, Category = DefaultExpenseCategories.Restaurants }
            };

            Assert.That(() => SplitTransactionValidator.Validate(-50, splits), Throws.Nothing);
            Assert.That(() => SplitTransactionValidator.Validate(-40, splits), Throws.InvalidOperationException);

            var transfer = new List<SplitTransactionRow>
            {
                new() { Amount = -50, Type = SplitType.Transfer }
            };
            Assert.That(() => SplitTransactionValidator.Validate(-50, transfer), Throws.InvalidOperationException);

            transfer[0].TransferAccountId = Guid.NewGuid();
            Assert.That(() => SplitTransactionValidator.Validate(-50, transfer), Throws.Nothing);
        }

        [Test]
        public void ApplySplits_PersistsAndClears_OnPostedTransaction()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionOrchestrator(store);
            var accountId = Guid.NewGuid();
            var posted = new PostedTransaction
            {
                AccountId = accountId,
                Date = new DateTime(2026, 3, 1),
                Description = "HEB",
                Amount = -80,
                CategoryId = DefaultExpenseCategories.GroceriesId,
                Category = DefaultExpenseCategories.Groceries
            };
            store.AddPostedTransaction(posted);

            orchestrator.ApplySplits(posted.Id,
            [
                new() { Amount = -50, Type = SplitType.Expense, CategoryId = DefaultExpenseCategories.GroceriesId, Category = DefaultExpenseCategories.Groceries },
                new() { Amount = -30, Type = SplitType.Expense, CategoryId = DefaultExpenseCategories.RestaurantsId, Category = DefaultExpenseCategories.Restaurants }
            ]);

            var loaded = store.GetPostedTransaction(posted.Id);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.HasSplits, Is.True);
            Assert.That(loaded.Splits, Has.Count.EqualTo(2));
            Assert.That(orchestrator.GetSplits(posted.Id), Has.Count.EqualTo(2));

            orchestrator.ClearSplits(posted.Id);
            Assert.That(store.GetPostedTransaction(posted.Id)!.HasSplits, Is.False);
        }

        [Test]
        public void ComputeActualExpenses_UsesSplitRows_IgnoresPrincipalAndTransfers()
        {
            var groceries = DefaultExpenseCategories.GroceriesId;
            var payment = DefaultExpenseCategories.PaymentId;
            var posted = new List<PostedTransaction>
            {
                new()
                {
                    Date = new DateTime(2026, 3, 1),
                    Amount = -80,
                    CategoryId = groceries,
                    Splits =
                    [
                        new() { Amount = -50, Type = SplitType.Expense, CategoryId = groceries, Category = DefaultExpenseCategories.Groceries },
                        new() { Amount = -30, Type = SplitType.Expense, CategoryId = DefaultExpenseCategories.RestaurantsId, Category = DefaultExpenseCategories.Restaurants }
                    ]
                },
                new()
                {
                    Date = new DateTime(2026, 3, 2),
                    Amount = -1000,
                    CategoryId = payment,
                    Splits =
                    [
                        new() { Amount = -700, Type = SplitType.Principal, CategoryId = payment, Category = DefaultExpenseCategories.Payment },
                        new() { Amount = -300, Type = SplitType.Interest, CategoryId = payment, Category = DefaultExpenseCategories.Payment }
                    ]
                },
                new()
                {
                    Date = new DateTime(2026, 3, 3),
                    Amount = 2000,
                    CategoryId = payment,
                    Splits =
                    [
                        new() { Amount = 1800, Type = SplitType.Income, CategoryId = DefaultExpenseCategories.UncategorizedId, Category = DefaultExpenseCategories.Uncategorized },
                        new() { Amount = 200, Type = SplitType.Transfer, TransferAccountId = Guid.NewGuid() }
                    ]
                }
            };

            var detector = new ExpenseBudgetDetector();
            var groceryActual = detector.ComputeActualExpenses(
                posted, [groceries], new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
            var paymentActual = detector.ComputeActualExpenses(
                posted, [payment], new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

            Assert.That(groceryActual, Is.EqualTo(50m));
            Assert.That(paymentActual, Is.EqualTo(300m));
        }

        [Test]
        public void UnifiedTransactionViewBuilder_ExpandsSplitRows()
        {
            var account = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var views = UnifiedTransactionViewBuilder.Build(
                [
                    new PostedTransaction
                    {
                        Id = parentId,
                        AccountId = account,
                        Date = new DateTime(2026, 1, 5),
                        Description = "HEB",
                        Amount = -80,
                        Splits =
                        [
                            new() { Id = Guid.NewGuid(), Amount = -50, Type = SplitType.Expense, Category = "Groceries", CategoryId = DefaultExpenseCategories.GroceriesId },
                            new() { Id = Guid.NewGuid(), Amount = -30, Type = SplitType.Expense, Category = "Pharmacy", CategoryId = DefaultExpenseCategories.RestaurantsId }
                        ]
                    }
                ],
                []);

            Assert.That(views, Has.Count.EqualTo(2));
            Assert.That(views.All(v => v.ParentTransactionId == parentId), Is.True);
            Assert.That(views.All(v => v.IsSplitRow), Is.True);
            Assert.That(views.All(v => v.Type == UnifiedTransactionView.PostedType), Is.True);
            Assert.That(views.Select(v => v.Amount).OrderBy(a => a), Is.EqualTo(new[] { -50m, -30m }.OrderBy(a => a)));
            Assert.That(views.Sum(v => v.Amount), Is.EqualTo(-80m));
        }

        [Test]
        public void ForecastGenerator_CopiesRuleSplits()
        {
            var accountId = Guid.NewGuid();
            var today = DateTime.Today;
            var ruleId = Guid.NewGuid();
            var rules = new List<RecurringSingleTransactionRule>
            {
                new()
                {
                    Id = ruleId,
                    IsActive = true,
                    AccountId = accountId,
                    Amount = -1000,
                    Frequency = RecurrenceFrequency.Monthly,
                    NextOccurrence = today,
                    EndDate = today,
                    Description = "Mortgage",
                    Splits =
                    [
                        new() { Amount = -700, Type = SplitType.Principal, CategoryId = DefaultExpenseCategories.PaymentId, Category = DefaultExpenseCategories.Payment },
                        new() { Amount = -300, Type = SplitType.Interest, CategoryId = DefaultExpenseCategories.PaymentId, Category = DefaultExpenseCategories.Payment }
                    ]
                }
            };

            var futures = new ForecastGenerator().GenerateForecast(accountId, today, today, rules, []);
            Assert.That(futures, Has.Count.EqualTo(2));
            Assert.That(futures.All(f => f.ParentTransactionId == ruleId), Is.True);
            Assert.That(futures.Sum(f => f.Amount), Is.EqualTo(-1000m));
            Assert.That(futures.Select(f => f.SplitKind), Does.Contain("Principal").And.Contain("Interest"));
        }

        [Test]
        public void LoanAmortizationEngine_SummarizesPrincipalAndInterestSplits()
        {
            var summary = LoanAmortizationEngine.SummarizePayments(200_000,
            [
                new() { Amount = -700, Type = SplitType.Principal },
                new() { Amount = -300, Type = SplitType.Interest },
                new() { Amount = -50, Type = SplitType.Expense }
            ]);

            Assert.That(summary.PrincipalPaid, Is.EqualTo(700m));
            Assert.That(summary.InterestPaid, Is.EqualTo(300m));
            Assert.That(summary.RemainingPrincipal, Is.EqualTo(199_300m));
        }
    }
}
