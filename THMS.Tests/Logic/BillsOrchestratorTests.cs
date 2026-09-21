using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class BillsOrchestratorTests
    {
        [Test]
        public void GetBills_DedupsStatementOverMatchingTransfer()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            statements.Save(new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-15),
                DueDate = DateTime.Today.AddDays(7),
                AmountDue = 220,
                StatementBalance = 220
            });
            transactions.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = checking.Id,
                ToAccountId = card.Id,
                Description = "Card payment",
                Amount = 125,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(6),
                IsActive = true
            });

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(card.Id, DateTime.Today);
            var checkingRows = new BillsOrchestrator(accounts, transactions, statements).GetBills(checking.Id, DateTime.Today);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Kind, Is.EqualTo(BillKinds.Statement));
            Assert.That(rows[0].Amount, Is.EqualTo(220m));
            Assert.That(rows[0].DestinationAccountId, Is.EqualTo(card.Id));
            Assert.That(rows[0].FundingAccountId, Is.EqualTo(Guid.Empty));
            Assert.That(rows[0].OtherAccountName, Is.EqualTo(""));
            Assert.That(checkingRows, Has.Count.EqualTo(1));
            Assert.That(checkingRows[0].Kind, Is.EqualTo(BillKinds.Transfer));
            Assert.That(checkingRows[0].Amount, Is.EqualTo(-125m));
            Assert.That(checkingRows[0].OtherAccountId, Is.EqualTo(card.Id));
        }

        [Test]
        public void GetBills_IncludesRecurringExpensesAndBankTransfers()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var hsa = new BankAccount { Name = "HSA", Institution = "X", AccountNumber = "3", WebsiteUrl = "" };
            accounts.UpsertAccount(hsa);
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Comcast",
                Amount = -80,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(3),
                IsActive = true
            });
            transactions.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = checking.Id,
                ToAccountId = hsa.Id,
                Description = "HSA Transfer",
                Amount = 448,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(4),
                IsActive = true
            });

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(checking.Id, DateTime.Today);
            var hsaRows = new BillsOrchestrator(accounts, transactions, statements).GetBills(hsa.Id, DateTime.Today);

            Assert.That(rows, Has.Some.Matches<BillRow>(r => r.Notes == "Comcast" && r.Kind == BillKinds.Recurring && r.Amount == -80m));
            Assert.That(rows, Has.Some.Matches<BillRow>(r => r.Notes == "HSA Transfer" && r.Kind == BillKinds.Transfer && r.Amount == -448m && r.OtherAccountId == hsa.Id));
            Assert.That(hsaRows, Has.Some.Matches<BillRow>(r => r.Notes == "HSA Transfer" && r.Amount == 448m && r.OtherAccountId == checking.Id));
        }

        [Test]
        public void GetPayFromChoices_StartsWithBlank()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            var choices = new BillsOrchestrator(accounts, transactions, statements).GetPayFromChoices();

            Assert.That(choices[0].Id, Is.EqualTo(Guid.Empty));
            Assert.That(choices[0].Name, Is.EqualTo(""));
            Assert.That(choices.Any(c => c.Id == checking.Id && c.Name == checking.Name), Is.True);
            Assert.That(choices.Any(c => c.Id == card.Id), Is.True);
        }

        [Test]
        public void Schedule_HoldsCashAndDoesNotPost()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            checking.PostedBalance = 1000;
            accounts.UpsertAccount(checking);
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Rent",
                Amount = -1850,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(5),
                IsActive = true
            });
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var rows = orchestrator.GetBills(checking.Id, DateTime.Today);
            rows[0].Pay = true;

            Assert.That(orchestrator.CashRemaining(), Is.EqualTo(1000m));
            Assert.That(orchestrator.CashRemaining(rows), Is.EqualTo(-850m));
            var scheduled = orchestrator.Schedule(rows);
            Assert.That(scheduled, Has.Count.EqualTo(1));
            Assert.That(transactions.GetPostedTransactions(checking.Id), Is.Empty);
            Assert.That(orchestrator.CashRemaining(), Is.EqualTo(-850m));
            Assert.That(orchestrator.GetBills(checking.Id, DateTime.Today).Single().Status, Is.EqualTo(BillStatuses.Scheduled));
        }

        [Test]
        public void MatchScheduled_ClearsIntentAndAdvancesRule()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var due = DateTime.Today.AddDays(2);
            var rule = new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Spotify",
                Amount = -11.96m,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = due,
                IsActive = true,
                IsUserCreated = true
            };
            transactions.AddRecurringSingleRule(rule);
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var row = orchestrator.GetBills(checking.Id, DateTime.Today).Single();
            row.Pay = true;
            orchestrator.Schedule([row]);

            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = due.AddDays(1),
                Amount = -11.96m,
                Description = "SPOTIFY"
            });

            Assert.That(orchestrator.MatchScheduled(), Is.EqualTo(1));
            var posted = transactions.GetPostedTransactions(checking.Id).Single();
            Assert.That(posted.RecommendedExpectedId, Is.Not.Null);
            Assert.That(posted.ImportedStatus, Is.EqualTo(ImportedStatus.Unreconciled));
            var updated = transactions.GetRecurringSingleRule(rule.Id)!;
            Assert.That(updated.LastOccurrence, Is.Null);
            new ReconciliationOrchestrator(transactions).AcceptMatch(posted.Id);
            updated = transactions.GetRecurringSingleRule(rule.Id)!;
            Assert.That(updated.LastOccurrence, Is.EqualTo(due.AddDays(1)));
            Assert.That(updated.NextOccurrence, Is.EqualTo(due.AddDays(1).AddMonths(1)));
            Assert.That(orchestrator.GetBills(checking.Id, DateTime.Today).Any(r => r.Notes == "Spotify" && r.Status == BillStatuses.Scheduled), Is.False);
        }

        [Test]
        public void Unschedule_ReturnsBillToDue()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Zoo",
                Amount = -20,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(1),
                IsActive = true
            });
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var row = orchestrator.GetBills(checking.Id, DateTime.Today).Single();
            row.Pay = true;
            var intent = orchestrator.Schedule([row]).Single();
            orchestrator.Unschedule(intent);

            var bills = orchestrator.GetBills(checking.Id, DateTime.Today);
            Assert.That(bills, Has.Count.EqualTo(1));
            Assert.That(bills[0].Status, Is.EqualTo(BillStatuses.Due));
        }

        [Test]
        public void GetBills_IncludesOverdueRecurringExpense()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Overdue donation",
                Amount = -25,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(-10),
                IsActive = true
            });

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(checking.Id, DateTime.Today);

            Assert.That(rows, Has.Some.Matches<BillRow>(r => r.Notes == "Overdue donation" && r.Amount == -25m && r.Status == BillStatuses.Pending));
        }

        [Test]
        public void GenerateForecast_SkipsOccurrenceCoveredByScheduledIntent()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var due = DateTime.Today.AddDays(3);
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Gym",
                Amount = -40,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = due,
                IsActive = true
            });
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var row = orchestrator.GetBills(checking.Id, DateTime.Today).Single();
            row.Pay = true;
            orchestrator.Schedule([row]);

            var forecast = new TransactionOrchestrator(transactions).GenerateForecast(
                checking.Id, DateTime.Today, DateTime.Today.AddDays(40));

            Assert.That(forecast.Any(v => v.Date.Date == due.Date && Math.Abs(v.Amount) == 40m), Is.False);
            Assert.That(forecast.Any(v => v.Date.Date == due.AddMonths(1).Date && Math.Abs(v.Amount) == 40m), Is.True);
        }

        [Test]
        public void AddManual_ForStatementCoversDueBill()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            var statement = new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-15),
                DueDate = DateTime.Today.AddDays(7),
                AmountDue = 220,
                StatementBalance = 220
            };
            statements.Save(statement);
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);

            orchestrator.AddManual(card.Id, checking.Id, 220, statement.DueDate, "Card payment", statement.Id);

            var rows = orchestrator.GetBills(card.Id, DateTime.Today);
            var checkingRows = orchestrator.GetBills(checking.Id, DateTime.Today);
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Status, Is.EqualTo(BillStatuses.Scheduled));
            Assert.That(rows[0].FundingAccountId, Is.EqualTo(checking.Id));
            Assert.That(rows[0].Amount, Is.EqualTo(220m));
            Assert.That(rows[0].OtherAccountId, Is.EqualTo(checking.Id));
            Assert.That(rows[0].SourceId, Is.EqualTo(statement.Id));
            Assert.That(checkingRows, Has.Count.EqualTo(1));
            Assert.That(checkingRows[0].Amount, Is.EqualTo(-220m));
            Assert.That(checkingRows[0].OtherAccountId, Is.EqualTo(card.Id));
        }

        [Test]
        public void GetBills_UsesOnlyLatestStatementPerAccount()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var mortgage = new MortgageAccount
            {
                Name = "Mortgage: RoundPoint",
                WebsiteUrl = "https://roundpoint.example"
            };
            accounts.UpsertAccount(mortgage);
            statements.Save(new MortgageStatement
            {
                AccountId = mortgage.Id,
                StatementDate = new DateTime(2026, 7, 3),
                DueDate = new DateTime(2026, 8, 1),
                AmountDue = 3257.20m,
                StatementBalance = 39700
            });
            statements.Save(new MortgageStatement
            {
                AccountId = mortgage.Id,
                StatementDate = new DateTime(2026, 8, 3),
                DueDate = new DateTime(2026, 9, 1),
                AmountDue = 3257.20m,
                StatementBalance = 39650
            });
            statements.Save(new MortgageStatement
            {
                AccountId = mortgage.Id,
                StatementDate = new DateTime(2026, 9, 3),
                DueDate = new DateTime(2026, 10, 1),
                AmountDue = 3257.20m,
                StatementBalance = 39594.91m
            });

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(mortgage.Id, DateTime.Today);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].DueDate, Is.EqualTo(new DateTime(2026, 10, 1)));
            Assert.That(rows[0].Amount, Is.EqualTo(3257.20m));
            Assert.That(rows[0].Kind, Is.EqualTo(BillKinds.Statement));
            Assert.That(rows[0].WebsiteUrl, Is.EqualTo("https://roundpoint.example"));
        }

        [Test]
        public void GetBills_DropsScheduledIntentsForOlderStatements()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var mortgage = new MortgageAccount { Name = "Mortgage: RoundPoint", WebsiteUrl = "" };
            accounts.UpsertAccount(mortgage);
            var older = new MortgageStatement
            {
                AccountId = mortgage.Id,
                StatementDate = new DateTime(2026, 8, 3),
                DueDate = new DateTime(2026, 9, 1),
                AmountDue = 3257.20m,
                StatementBalance = 39650
            };
            var latest = new MortgageStatement
            {
                AccountId = mortgage.Id,
                StatementDate = new DateTime(2026, 9, 3),
                DueDate = new DateTime(2026, 10, 1),
                AmountDue = 3257.20m,
                StatementBalance = 39594.91m
            };
            statements.Save(older);
            statements.Save(latest);
            transactions.AddFutureTransferTransaction(new FutureTransferTransaction
            {
                FromAccountId = checking.Id,
                ToAccountId = mortgage.Id,
                Amount = older.AmountDue,
                Date = older.DueDate,
                Origin = ExpectedOrigin.StatementPay,
                OriginId = older.Id,
                Status = ExpectedStatus.Scheduled,
                IsUserCreated = true,
                Description = mortgage.Name
            });
            transactions.AddFutureTransferTransaction(new FutureTransferTransaction
            {
                FromAccountId = checking.Id,
                ToAccountId = mortgage.Id,
                Amount = latest.AmountDue,
                Date = latest.DueDate,
                Origin = ExpectedOrigin.StatementPay,
                OriginId = latest.Id,
                Status = ExpectedStatus.Scheduled,
                IsUserCreated = true,
                Description = mortgage.Name
            });

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(mortgage.Id, DateTime.Today);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].DueDate, Is.EqualTo(latest.DueDate));
            Assert.That(rows[0].Status, Is.EqualTo(BillStatuses.Scheduled));
            Assert.That(transactions.GetAllFutureTransferTransactions().Count(e => !e.IsRealized), Is.EqualTo(1));
            Assert.That(transactions.GetAllFutureTransferTransactions().Single(e => !e.IsRealized).OriginId, Is.EqualTo(latest.Id));
        }

        [Test]
        public void GetBills_IncludesManualPendingChargeOnThatAccountOnly()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            new AccountActivityOrchestrator(transactions, transactions).AddPending(
                card.Id, DateTime.Today.AddDays(1), -42.50m, "Coffee", DefaultExpenseCategories.RestaurantsId);

            var cardRows = new BillsOrchestrator(accounts, transactions, statements).GetBills(card.Id, DateTime.Today);
            var checkingRows = new BillsOrchestrator(accounts, transactions, statements).GetBills(checking.Id, DateTime.Today);

            Assert.That(cardRows, Has.Some.Matches<BillRow>(r => r.Notes == "Coffee" && r.Amount == -42.50m && r.Kind == BillKinds.Manual));
            Assert.That(checkingRows, Has.None.Matches<BillRow>(r => r.Notes == "Coffee"));
        }

        [Test]
        public void GetBills_IncludesSplitTransferNetOnTheOtherAccount()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            var paycheck = new FutureSingleTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today.AddDays(2),
                Amount = 1000,
                Description = "Paycheck",
                Origin = ExpectedOrigin.Manual,
                Status = ExpectedStatus.Planned,
                IsUserCreated = true,
                Splits =
                [
                    new SplitTransactionRow
                    {
                        Amount = 1100,
                        Type = SplitType.Income,
                        CategoryId = DefaultExpenseCategories.UncategorizedId
                    },
                    new SplitTransactionRow
                    {
                        Amount = -100,
                        Type = SplitType.Transfer,
                        TransferAccountId = card.Id
                    }
                ]
            };
            transactions.AddFutureSingleTransaction(paycheck);

            var checkingRows = new BillsOrchestrator(accounts, transactions, statements).GetBills(checking.Id, DateTime.Today);
            var cardRows = new BillsOrchestrator(accounts, transactions, statements).GetBills(card.Id, DateTime.Today);

            Assert.That(checkingRows, Has.Some.Matches<BillRow>(r => r.Notes == "Paycheck" && r.Amount == 1000m));
            Assert.That(cardRows, Has.Some.Matches<BillRow>(r => r.Notes == "Paycheck" && r.Amount == 100m && r.OtherAccountId == checking.Id));
        }

        [Test]
        public void MatchToImported_MatchesDueBill()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var due = DateTime.Today.AddDays(1);
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Netflix",
                Amount = -15.99m,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = due,
                IsActive = true
            });
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var bill = orchestrator.GetBills(checking.Id, DateTime.Today).Single();
            var posted = new PostedTransaction
            {
                AccountId = checking.Id,
                Date = due,
                Amount = -15.99m,
                Description = "NETFLIX.COM",
                ImportedStatus = ImportedStatus.Unreconciled
            };
            transactions.AddPostedTransaction(posted);

            orchestrator.MatchToImported(bill, posted.Id);

            Assert.That(transactions.GetPostedTransaction(posted.Id)!.ImportedStatus, Is.EqualTo(ImportedStatus.Matched));
            Assert.That(orchestrator.GetBills(checking.Id, DateTime.Today)
                .Any(r => r.Notes == "Netflix" && r.DueDate == due), Is.False);
        }

        private static (InMemoryAccountDataStore accounts, InMemoryTransactionDataStore transactions, InMemoryAccountStatementDataStore statements, BankAccount checking, CreditAccount card) SeedCheckingAndCard()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = "",
                PostedBalance = 0
            };
            var card = new CreditAccount
            {
                Name = "Card",
                Institution = "X",
                AccountNumber = "2",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(card);
            return (accounts, transactions, statements, checking, card);
        }
    }
}
