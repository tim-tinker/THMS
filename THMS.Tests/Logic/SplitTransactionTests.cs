using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Budget;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Transactions;
using THMS.Logic.Finance.Transfer;
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

            transfer[0].FromAccountId = Guid.NewGuid();
            transfer[0].ToAccountId = Guid.NewGuid();
            transfer[0].CategoryId = DefaultExpenseCategories.PaymentId;
            transfer[0].Category = DefaultExpenseCategories.Payment;
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
        public void UnifiedTransactionViewBuilder_SummarizesSplitRowsOnParentAccount()
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
                        ImportedStatus = ImportedStatus.AcceptedNew,
                        Splits =
                        [
                            new() { Id = Guid.NewGuid(), Amount = -50, Type = SplitType.Expense, Category = "Groceries", CategoryId = DefaultExpenseCategories.GroceriesId },
                            new() { Id = Guid.NewGuid(), Amount = -30, Type = SplitType.Expense, Category = "Pharmacy", CategoryId = DefaultExpenseCategories.RestaurantsId }
                        ]
                    }
                ],
                [],
                forAccountId: account);

            Assert.That(views, Has.Count.EqualTo(1));
            Assert.That(views[0].ParentTransactionId, Is.EqualTo(parentId));
            Assert.That(views[0].Amount, Is.EqualTo(-80m));
            Assert.That(views[0].Category, Is.EqualTo(UnifiedTransactionView.SplitCategory));
            Assert.That(views[0].IsSplitRow, Is.False);
            Assert.That(views[0].Type, Is.EqualTo(UnifiedTransactionView.PostedType));
            Assert.That(views[0].Description, Is.EqualTo("HEB"));
        }

        [Test]
        public void UnifiedTransactionViewBuilder_ProjectsTransferSplitsOntoDestinationAccount()
        {
            var checking = Guid.NewGuid();
            var retirement = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var transferSplitId = Guid.NewGuid();
            var paycheck = new PostedTransaction
            {
                Id = parentId,
                AccountId = checking,
                Date = new DateTime(2026, 1, 5),
                Description = "Paycheck",
                Amount = 1234,
                ImportedStatus = ImportedStatus.AcceptedNew,
                Splits =
                [
                    new() { Amount = 2345, Type = SplitType.Income },
                    new() { Id = transferSplitId, Amount = -500, Type = SplitType.Transfer, TransferAccountId = retirement },
                    new() { Amount = -123, Type = SplitType.Expense },
                    new() { Amount = -488, Type = SplitType.Expense }
                ]
            };

            var checkingViews = UnifiedTransactionViewBuilder.Build([paycheck], [], forAccountId: checking);
            Assert.That(checkingViews, Has.Count.EqualTo(1));
            Assert.That(checkingViews[0].Amount, Is.EqualTo(1234m));
            Assert.That(checkingViews[0].Category, Is.EqualTo(UnifiedTransactionView.SplitCategory));
            Assert.That(checkingViews.All(v => v.Amount != -500m && v.Amount != 2345m), Is.True);

            var retirementViews = UnifiedTransactionViewBuilder.Build(
                [],
                [],
                forAccountId: retirement,
                incomingPostedSplitSources: [paycheck]);
            Assert.That(retirementViews, Has.Count.EqualTo(1));
            Assert.That(retirementViews[0].Amount, Is.EqualTo(500m));
            Assert.That(retirementViews[0].AccountId, Is.EqualTo(retirement));
            Assert.That(retirementViews[0].ParentTransactionId, Is.EqualTo(parentId));
            Assert.That(retirementViews[0].SplitRowId, Is.EqualTo(transferSplitId));
            Assert.That(retirementViews[0].Type, Is.EqualTo(UnifiedTransactionView.PostedTransferType));
            Assert.That(retirementViews[0].TypeLabel, Is.EqualTo("Transfer"));
            Assert.That(retirementViews[0].Description, Is.EqualTo("Paycheck"));
        }

        [Test]
        public void ForecastGenerator_SummarizesRuleSplitsAsOneRow()
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
                    Category = "Housing",
                    Splits =
                    [
                        new() { Amount = -700, Type = SplitType.Principal, CategoryId = DefaultExpenseCategories.PaymentId, Category = DefaultExpenseCategories.Payment },
                        new() { Amount = -300, Type = SplitType.Interest, CategoryId = DefaultExpenseCategories.PaymentId, Category = DefaultExpenseCategories.Payment }
                    ]
                }
            };

            var futures = new ForecastGenerator().GenerateForecast(accountId, today, today, rules, []);
            Assert.That(futures, Has.Count.EqualTo(1));
            Assert.That(futures[0].ParentTransactionId, Is.EqualTo(ruleId));
            Assert.That(futures[0].Amount, Is.EqualTo(-1000m));
            Assert.That(futures[0].Category, Is.EqualTo(UnifiedTransactionView.SplitCategory));
            Assert.That(futures[0].CategoryId, Is.Null);
            Assert.That(futures[0].IsSplitRow, Is.False);
            Assert.That(futures[0].Type, Is.EqualTo(UnifiedTransactionView.ForecastType));
            Assert.That(futures[0].Description, Is.EqualTo("Mortgage"));
        }

        [Test]
        public void UnifiedTransactionViewBuilder_BuildRecurringRules_SummarizesSplits()
        {
            var accountId = Guid.NewGuid();
            var views = UnifiedTransactionViewBuilder.BuildRecurringRules(
                [
                    new RecurringSingleTransactionRule
                    {
                        AccountId = accountId,
                        Description = "Paycheck",
                        Amount = 1234,
                        Category = "Income",
                        NextOccurrence = new DateTime(2026, 2, 1),
                        Splits =
                        [
                            new() { Amount = 2345, Type = SplitType.Income },
                            new() { Amount = -500, Type = SplitType.Transfer },
                            new() { Amount = -123, Type = SplitType.Expense },
                            new() { Amount = -488, Type = SplitType.Expense }
                        ]
                    }
                ],
                []);

            Assert.That(views, Has.Count.EqualTo(1));
            Assert.That(views[0].Amount, Is.EqualTo(1234m));
            Assert.That(views[0].Category, Is.EqualTo(UnifiedTransactionView.SplitCategory));
            Assert.That(views[0].Type, Is.EqualTo(UnifiedTransactionView.RecurringRuleType));
            Assert.That(views[0].Description, Is.EqualTo("Paycheck"));
            Assert.That(views[0].IsSplitRow, Is.False);
        }

        [Test]
        public void UnifiedTransactionViewBuilder_BuildCategoryRows_ExpandsSplits()
        {
            var checking = Guid.NewGuid();
            var retirement = Guid.NewGuid();
            var taxId = Guid.NewGuid();
            var paycheck = new PostedTransaction
            {
                AccountId = checking,
                Date = new DateTime(2026, 1, 5),
                Description = "Paycheck",
                Amount = 1234,
                ImportedStatus = ImportedStatus.AcceptedNew,
                Splits =
                [
                    new() { Amount = 2345, Type = SplitType.Income, CategoryId = DefaultExpenseCategories.UncategorizedId, Category = DefaultExpenseCategories.Uncategorized },
                    new() { Amount = -500, Type = SplitType.Transfer, TransferAccountId = retirement },
                    new() { Amount = -123, Type = SplitType.Expense, CategoryId = taxId, Category = "Federal Tax" },
                    new() { Amount = -488, Type = SplitType.Expense }
                ]
            };

            var ledger = UnifiedTransactionViewBuilder.Build([paycheck], [], forAccountId: checking);
            Assert.That(ledger, Has.Count.EqualTo(1));
            Assert.That(ledger[0].Amount, Is.EqualTo(1234m));

            var rows = UnifiedTransactionViewBuilder.BuildCategoryRows([paycheck]);
            Assert.That(rows, Has.Count.EqualTo(4));
            Assert.That(rows.Sum(r => r.Amount), Is.EqualTo(1234m));
            Assert.That(rows.Single(r => r.SplitKind == nameof(SplitType.Transfer)).Amount, Is.EqualTo(-500m));
            Assert.That(rows.Single(r => r.Category == "Federal Tax").Amount, Is.EqualTo(-123m));

            var tax = UnifiedTransactionViewBuilder.FilterCategoryRows(
                rows, new CategoryFilterChoice("Tax", taxId), [new ExpenseCategory { Id = taxId, Name = "Federal Tax" }]);
            Assert.That(tax, Has.Count.EqualTo(1));
            Assert.That(tax[0].Amount, Is.EqualTo(-123m));

            var uncategorized = UnifiedTransactionViewBuilder.FilterCategoryRows(
                rows, CategoryFilterChoice.Uncategorized, []);
            Assert.That(uncategorized.Select(r => r.Amount).OrderBy(a => a), Is.EqualTo(new[] { -488m, 2345m }.OrderBy(a => a)));
        }

        [Test]
        public void ForecastGenerator_ProjectsTransferSplitsOntoDestinationAccount()
        {
            var checking = Guid.NewGuid();
            var savings = Guid.NewGuid();
            var today = DateTime.Today;
            var ruleId = Guid.NewGuid();
            var rules = new List<RecurringSingleTransactionRule>
            {
                new()
                {
                    Id = ruleId,
                    IsActive = true,
                    AccountId = checking,
                    Amount = 1234,
                    Frequency = RecurrenceFrequency.Monthly,
                    NextOccurrence = today,
                    EndDate = today,
                    Description = "Paycheck",
                    Splits =
                    [
                        new() { Amount = 2345, Type = SplitType.Income },
                        new() { Amount = -500, Type = SplitType.Transfer, TransferAccountId = savings },
                        new() { Amount = -123, Type = SplitType.Expense },
                        new() { Amount = -488, Type = SplitType.Expense }
                    ]
                }
            };

            var checkingForecast = new ForecastGenerator().GenerateForecast(checking, today, today, rules, []);
            Assert.That(checkingForecast, Has.Count.EqualTo(1));
            Assert.That(checkingForecast[0].Amount, Is.EqualTo(1234m));
            Assert.That(checkingForecast[0].Category, Is.EqualTo(UnifiedTransactionView.SplitCategory));

            var savingsForecast = new ForecastGenerator().GenerateForecast(savings, today, today, rules, []);
            Assert.That(savingsForecast, Has.Count.EqualTo(1));
            Assert.That(savingsForecast[0].Amount, Is.EqualTo(500m));
            Assert.That(savingsForecast[0].Type, Is.EqualTo(UnifiedTransactionView.ForecastTransferType));
            Assert.That(savingsForecast[0].ParentTransactionId, Is.EqualTo(ruleId));
            Assert.That(savingsForecast[0].Description, Is.EqualTo("Paycheck"));
        }

        [Test]
        public void TransactionOrchestrator_IncludesIncomingTransferSplitsOnDestination()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionOrchestrator(store);
            var checking = Guid.NewGuid();
            var savings = Guid.NewGuid();
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking,
                Date = new DateTime(2026, 1, 5),
                Description = "Paycheck",
                Amount = 1234,
                Splits =
                [
                    new() { Amount = 2345, Type = SplitType.Income },
                    new() { Amount = -500, Type = SplitType.Transfer, TransferAccountId = savings },
                    new() { Amount = -123, Type = SplitType.Expense },
                    new() { Amount = -488, Type = SplitType.Expense }
                ]
            });

            var checkingTxs = orchestrator.GetTransactionsForAccount(checking);
            Assert.That(checkingTxs.IncomingTransferSplitPosted, Is.Empty);

            var savingsTxs = orchestrator.GetTransactionsForAccount(savings);
            Assert.That(savingsTxs.IncomingTransferSplitPosted.Count(), Is.EqualTo(1));
            Assert.That(orchestrator.ComputePostedBalance(savings, 0), Is.EqualTo(500m));
            Assert.That(orchestrator.SumPostedAmountsBefore(savings, new DateTime(2026, 1, 6)), Is.EqualTo(500m));
            Assert.That(orchestrator.SumPostedAmountsBefore(savings, new DateTime(2026, 1, 5)), Is.EqualTo(0m));
            Assert.That(orchestrator.GetParent(savingsTxs.IncomingTransferSplitPosted.First().Id)!.Amount, Is.EqualTo(1234m));
        }

        [Test]
        public void WholeAmountTransfer_KeepsCategoryInsteadOfShowingSplit()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionOrchestrator(store);
            var savings = Guid.NewGuid();
            var checking = Guid.NewGuid();
            var transferCategory = Guid.NewGuid();
            var transportation = Guid.NewGuid();
            var savingsTx = new PostedTransaction
            {
                AccountId = savings,
                Date = new DateTime(2026, 9, 8),
                Amount = -1000,
                Description = "ONLINE TRANSFER TO TINKER T EVERYDAY CHECKING",
                Category = "Transfer",
                CategoryId = transferCategory,
                ImportedStatus = ImportedStatus.Matched
            };
            var checkingTx = new PostedTransaction
            {
                AccountId = checking,
                Date = new DateTime(2026, 9, 8),
                Amount = 1000,
                Description = "ONLINE TRANSFER FROM WAY2SAVE",
                Category = "Transportation",
                CategoryId = transportation,
                ImportedStatus = ImportedStatus.Matched
            };
            store.AddPostedTransaction(savingsTx);
            store.AddPostedTransaction(checkingTx);

            var row = new SplitTransactionRow
            {
                Amount = -1000,
                Type = SplitType.Transfer,
                Category = "Transfer",
                CategoryId = transferCategory,
                FromAccountId = savings,
                ToAccountId = checking
            };
            orchestrator.ApplySplits(savingsTx.Id, [row]);
            var splitView = UnifiedTransactionViewBuilder.Build(
                store.GetPostedTransactions(savings),
                store.GetPostedTransferTransactions(savings),
                forAccountId: savings).Single();
            Assert.That(splitView.Category, Is.EqualTo(UnifiedTransactionView.SplitCategory));

            Assert.That(orchestrator.TryApplyAsWholeTransfer(savingsTx.Id, [row.Clone()]), Is.True);

            Assert.That(store.GetPostedTransaction(savingsTx.Id), Is.Null);
            var transfer = store.GetPostedTransferTransaction(savingsTx.Id);
            Assert.That(transfer, Is.Not.Null);
            Assert.That(transfer!.Category, Is.EqualTo("Transfer"));
            Assert.That(transfer.HasSplits, Is.False);
            Assert.That(transfer.FromAccountId, Is.EqualTo(savings));
            Assert.That(transfer.ToAccountId, Is.EqualTo(checking));
            Assert.That(transfer.Direction, Is.EqualTo(TransferDirection.Outgoing));

            var other = store.GetPostedTransferTransaction(checkingTx.Id);
            Assert.That(other, Is.Not.Null);
            Assert.That(other!.Category, Is.EqualTo("Transportation"));
            Assert.That(other.FromAccountId, Is.EqualTo(savings));
            Assert.That(other.ToAccountId, Is.EqualTo(checking));
            Assert.That(other.Direction, Is.EqualTo(TransferDirection.Incoming));
            Assert.That(transfer.RelatedPostedTransactionId, Is.EqualTo(checkingTx.Id));
            Assert.That(other.RelatedPostedTransactionId, Is.EqualTo(savingsTx.Id));

            var view = UnifiedTransactionViewBuilder.Build(
                store.GetPostedTransactions(savings),
                store.GetPostedTransferTransactions(savings),
                forAccountId: savings).Single();
            Assert.That(view.Category, Is.EqualTo("Transfer"));
        }

        [Test]
        public void Seed_KeepsCategoryWithoutTreatingTransferCategoryAsTransferType()
        {
            var transferCategory = new ExpenseCategory { Id = Guid.NewGuid(), Name = "Transfer" };
            var posted = new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Date = new DateTime(2026, 9, 8),
                Amount = -1000,
                Description = "ONLINE TRANSFER TO SAVINGS",
                Category = "Transfer",
                CategoryId = transferCategory.Id
            };

            var seed = SplitTransactionSeed.Create(posted, transferAccountId: Guid.NewGuid(), [transferCategory]);

            Assert.That(seed.Category, Is.EqualTo("Transfer"));
            Assert.That(seed.Type, Is.EqualTo(SplitType.Expense));
            Assert.That(seed.FromAccountId, Is.Null);
            Assert.That(seed.ToAccountId, Is.Null);
        }

        [Test]
        public void Seed_UsesFromAndToOnAPostedTransfer()
        {
            var checking = Guid.NewGuid();
            var savings = Guid.NewGuid();
            var transferCategory = new ExpenseCategory { Id = Guid.NewGuid(), Name = "Transfer" };
            var posted = new PostedTransferTransaction
            {
                AccountId = savings,
                Date = new DateTime(2026, 9, 8),
                Amount = -1000,
                Description = "ONLINE TRANSFER TO CHECKING",
                Category = "Transfer",
                CategoryId = transferCategory.Id,
                Direction = TransferDirection.Outgoing,
                FromAccountId = savings,
                ToAccountId = checking
            };

            var seed = SplitTransactionSeed.Create(posted, transferAccountId: null, [transferCategory]);

            Assert.That(seed.Type, Is.EqualTo(SplitType.Transfer));
            Assert.That(seed.CategoryId, Is.EqualTo(transferCategory.Id));
            Assert.That(seed.FromAccountId, Is.EqualTo(savings));
            Assert.That(seed.ToAccountId, Is.EqualTo(checking));
        }

        [Test]
        public void Seed_KeepsExpenseTypeWhenTheTransactionIsNotATransfer()
        {
            var groceries = new ExpenseCategory { Id = DefaultExpenseCategories.GroceriesId, Name = DefaultExpenseCategories.Groceries };
            var purchase = new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Date = new DateTime(2026, 9, 8),
                Amount = -81.88m,
                Description = "ZELLE FROM PANGELINAN ANNA",
                CategoryId = groceries.Id,
                Category = groceries.Name
            };

            var seed = SplitTransactionSeed.Create(purchase, transferAccountId: null, [groceries]);

            Assert.That(seed.Type, Is.EqualTo(SplitType.Expense));
            Assert.That(seed.CategoryId, Is.EqualTo(groceries.Id));
            Assert.That(seed.TransferAccountId, Is.Null);
        }

        [Test]
        public void ImportedTransferPairing_UsesSignForFromAndToAndRuleCategories()
        {
            var checking = Guid.NewGuid();
            var savings = Guid.NewGuid();
            var source = new ExpenseCategory { Id = Guid.NewGuid(), Name = "Savings transfer" };
            var target = new ExpenseCategory { Id = Guid.NewGuid(), Name = "Checking receipt" };
            var rule = new RecurringTransferRule
            {
                FromAccountId = savings,
                ToAccountId = checking,
                Amount = 1000,
                NextOccurrence = new DateTime(2026, 9, 8),
                IsActive = true,
                CategoryId = source.Id,
                Category = source.Name,
                TargetCategoryId = target.Id,
                TargetCategory = target.Name
            };
            var fromSide = new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = savings,
                Date = new DateTime(2026, 9, 8),
                Amount = -1000,
                Description = "RECURRING TRANSFER TO CHECKING"
            };
            var toSide = new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = checking,
                Date = new DateTime(2026, 9, 8),
                Amount = 1000,
                Description = "RECURRING TRANSFER FROM SAVINGS"
            };

            var pairs = ImportedTransferPairing.Find([fromSide], [toSide], [rule]);

            Assert.That(pairs, Has.Count.EqualTo(1));
            Assert.That(pairs[0].FromSide.AccountId, Is.EqualTo(savings));
            Assert.That(pairs[0].ToSide.AccountId, Is.EqualTo(checking));
            Assert.That(pairs[0].Rule, Is.SameAs(rule));
        }

        [Test]
        public void ImportedTransferPairing_DefaultsWhenNoRuleMatchesTheDescription()
        {
            var fromSide = new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Date = new DateTime(2026, 9, 4),
                Amount = -1000,
                Description = "ONLINE TRANSFER TO SAVINGS"
            };
            var toSide = new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Date = new DateTime(2026, 9, 4),
                Amount = 1000,
                Description = "ONLINE TRANSFER FROM CHECKING"
            };

            var pairs = ImportedTransferPairing.Find([fromSide], [toSide], []);

            Assert.That(pairs, Has.Count.EqualTo(1));
            Assert.That(pairs[0].Rule, Is.Null);
            Assert.That(pairs[0].FromSide.Id, Is.EqualTo(fromSide.Id));
            Assert.That(pairs[0].ToSide.Id, Is.EqualTo(toSide.Id));
        }

        [Test]
        public void ImportedTransferPairing_IgnoresACategoryNamedTransfer()
        {
            var fromSide = new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Date = new DateTime(2026, 9, 4),
                Amount = -40,
                Description = "GROCERY",
                Category = "Transfer"
            };
            var toSide = new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Date = new DateTime(2026, 9, 4),
                Amount = 40,
                Description = "PAYCHECK"
            };

            var pairs = ImportedTransferPairing.Find([fromSide], [toSide], []);

            Assert.That(pairs, Is.Empty);
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
