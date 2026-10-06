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
        public void Schedule_KeepsEveryBillDueOnTheSameDayOffTheLedger()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var due = DateTime.Today.AddDays(-4);
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Donation: St Laurence",
                Amount = -80,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = due,
                IsActive = true
            });
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Service: ARS Houston",
                Amount = -22,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = due,
                IsActive = true
            });
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Electric",
                Amount = -40,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = due,
                IsActive = true
            });
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var rows = orchestrator.GetBills(checking.Id, DateTime.Today);
            foreach (var row in rows)
                row.Pay = true;

            orchestrator.Schedule(rows);

            var bills = orchestrator.GetBills(checking.Id, DateTime.Today);
            Assert.That(bills.Select(r => r.Notes), Is.EquivalentTo(new[]
            {
                "Donation: St Laurence",
                "Service: ARS Houston",
                "Electric"
            }));
            Assert.That(bills.Select(r => r.Status), Is.All.EqualTo(BillStatuses.Scheduled));
            Assert.That(transactions.GetPostedTransactions(checking.Id), Is.Empty);
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
        public void CashRemaining_AddsPaychecksAndCountsOverdue()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            checking.PostedBalance = 1000;
            accounts.UpsertAccount(checking);
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Paycheck",
                Amount = 2000,
                Frequency = RecurrenceFrequency.BiWeekly,
                NextOccurrence = DateTime.Today.AddDays(2),
                IsActive = true
            });
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Overdue donation",
                Amount = -25,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(-10),
                IsActive = true
            });
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var rows = orchestrator.GetBills(checking.Id, DateTime.Today);
            foreach (var row in rows)
                row.Pay = true;

            Assert.That(orchestrator.CashRemaining(checking.Id, rows), Is.EqualTo(2975m));
        }

        [Test]
        public void CashRemaining_UsesSelectedBankStatementNotOtherBanks()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            checking.PostedBalance = 99999;
            accounts.UpsertAccount(checking);
            var savings = new BankAccount
            {
                Name = "Savings",
                Institution = "X",
                AccountNumber = "9",
                WebsiteUrl = "",
                PostedBalance = 8000
            };
            accounts.UpsertAccount(savings);
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = new DateTime(2026, 9, 8),
                DueDate = new DateTime(2026, 9, 8),
                StatementBalance = 1090.73m
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = new DateTime(2026, 9, 10),
                Amount = -40m,
                Description = "Coffee"
            });
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);

            Assert.That(orchestrator.CashRemaining(checking.Id), Is.EqualTo(1050.73m));
            Assert.That(orchestrator.CashRemaining(), Is.EqualTo(9050.73m));
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
        public void GetBills_RecordedChargeIsNotABill()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            new AccountActivityOrchestrator(transactions, transactions).AddPosted(
                card.Id, new DateTime(2026, 9, 28), -42.50m, "Coffee", DefaultExpenseCategories.RestaurantsId);

            var cardRows = new BillsOrchestrator(accounts, transactions, statements).GetBills(card.Id, DateTime.Today);
            var checkingRows = new BillsOrchestrator(accounts, transactions, statements).GetBills(checking.Id, DateTime.Today);

            Assert.That(cardRows, Has.None.Matches<BillRow>(r => r.Notes == "Coffee"));
            Assert.That(checkingRows, Has.None.Matches<BillRow>(r => r.Notes == "Coffee"));
            Assert.That(transactions.GetPostedTransactions(card.Id).Single().ImportedStatus, Is.EqualTo(ImportedStatus.Unmatched));
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

        [Test]
        public void AddEnteredBill_OneTimeWithoutCredit_IsATransactionOnTheBills()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var due = new DateTime(2026, 10, 20);

            orchestrator.AddEnteredBill(
                checking.Id, "Comcast", -80m, DefaultExpenseCategories.UtilityId, due,
                creditAccountId: null, creditCategoryId: null, frequency: null);

            var bill = orchestrator.GetBills(checking.Id, new DateTime(2026, 10, 2)).Single();
            Assert.That(bill.Notes, Is.EqualTo("Comcast"));
            Assert.That(bill.Amount, Is.EqualTo(-80m));
            Assert.That(bill.DueDate, Is.EqualTo(due));
            Assert.That(bill.Kind, Is.EqualTo(BillKinds.Manual));
            Assert.That(transactions.GetAllRecurringSingleRules(), Is.Empty);
            Assert.That(transactions.GetFutureSingleTransactions(checking.Id).Single().CategoryId,
                Is.EqualTo(DefaultExpenseCategories.UtilityId));
        }

        [Test]
        public void AddEnteredBill_OneTimeWithCredit_IsATransferOnTheBills()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var due = new DateTime(2026, 10, 20);

            orchestrator.AddEnteredBill(
                card.Id, "Promo installment", 150m, DefaultExpenseCategories.PaymentId, due,
                checking.Id, DefaultExpenseCategories.PaymentId, frequency: null);

            var cardBill = orchestrator.GetBills(card.Id, new DateTime(2026, 10, 2)).Single();
            var checkingBill = orchestrator.GetBills(checking.Id, new DateTime(2026, 10, 2)).Single();
            Assert.That(cardBill.Amount, Is.EqualTo(150m));
            Assert.That(cardBill.OtherAccountId, Is.EqualTo(checking.Id));
            Assert.That(checkingBill.Amount, Is.EqualTo(-150m));
            Assert.That(transactions.GetAllRecurringTransferRules(), Is.Empty);
            var transfer = transactions.GetAllFutureTransferTransactions().Single();
            Assert.That(transfer.FromAccountId, Is.EqualTo(checking.Id));
            Assert.That(transfer.ToAccountId, Is.EqualTo(card.Id));
            Assert.That(transfer.CategoryId, Is.EqualTo(DefaultExpenseCategories.PaymentId));
            Assert.That(transfer.TargetCategoryId, Is.EqualTo(DefaultExpenseCategories.PaymentId));
        }

        [Test]
        public void DeleteEnteredBill_RemovesOneTimeTransactionAndTransfer()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var due = new DateTime(2026, 10, 20);
            orchestrator.AddEnteredBill(
                checking.Id, "Comcast", -80m, DefaultExpenseCategories.UtilityId, due,
                creditAccountId: null, creditCategoryId: null, frequency: null);
            orchestrator.AddEnteredBill(
                card.Id, "Promo installment", 150m, DefaultExpenseCategories.PaymentId, due,
                checking.Id, DefaultExpenseCategories.PaymentId, frequency: null);

            var single = orchestrator.GetBills(checking.Id, new DateTime(2026, 10, 2)).Single(r => r.Notes == "Comcast");
            var transfer = orchestrator.GetBills(card.Id, new DateTime(2026, 10, 2)).Single();
            orchestrator.DeleteEnteredBill(single.IntentId!.Value);
            orchestrator.DeleteEnteredBill(transfer.IntentId!.Value);

            Assert.That(orchestrator.GetBills(checking.Id, new DateTime(2026, 10, 2)), Is.Empty);
            Assert.That(orchestrator.GetBills(card.Id, new DateTime(2026, 10, 2)), Is.Empty);
        }

        [Test]
        public void AddEnteredBill_RecurringWithoutCredit_IsASingleAccountBill()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var due = new DateTime(2026, 10, 20);

            orchestrator.AddEnteredBill(
                checking.Id, "Spotify", -11.96m, DefaultExpenseCategories.UncategorizedId, due,
                creditAccountId: null, creditCategoryId: null, RecurrenceFrequency.Monthly);

            var rule = transactions.GetAllRecurringSingleRules().Single();
            Assert.That(rule.AccountId, Is.EqualTo(checking.Id));
            Assert.That(rule.Frequency, Is.EqualTo(RecurrenceFrequency.Monthly));
            Assert.That(rule.Amount, Is.EqualTo(-11.96m));
            Assert.That(rule.CategoryId, Is.EqualTo(DefaultExpenseCategories.UncategorizedId));
            Assert.That(orchestrator.GetBills(checking.Id, new DateTime(2026, 10, 2)).Single().Notes, Is.EqualTo("Spotify"));
        }

        [Test]
        public void AddEnteredBill_RecurringWithCredit_IsATransferBillWithBothCategories()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var due = new DateTime(2026, 10, 20);

            orchestrator.AddEnteredBill(
                card.Id, "Promo installment", 75m, DefaultExpenseCategories.InterestId, due,
                checking.Id, DefaultExpenseCategories.PaymentId, RecurrenceFrequency.BiWeekly);

            var rule = transactions.GetAllRecurringTransferRules().Single();
            Assert.That(rule.FromAccountId, Is.EqualTo(checking.Id));
            Assert.That(rule.ToAccountId, Is.EqualTo(card.Id));
            Assert.That(rule.Frequency, Is.EqualTo(RecurrenceFrequency.BiWeekly));
            Assert.That(rule.Amount, Is.EqualTo(75m));
            Assert.That(rule.CategoryId, Is.EqualTo(DefaultExpenseCategories.PaymentId));
            Assert.That(rule.TargetCategoryId, Is.EqualTo(DefaultExpenseCategories.InterestId));
            Assert.That(orchestrator.GetBills(card.Id, new DateTime(2026, 10, 2)), Has.Count.EqualTo(1));
            Assert.That(orchestrator.GetBills(checking.Id, new DateTime(2026, 10, 2)), Has.Count.EqualTo(1));
        }

        [Test]
        public void SavePaymentPlan_KeepsOneTransferAndShowsItOnBills()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);
            var next = new DateTime(2026, 10, 9);
            var end = new DateTime(2027, 4, 9);

            orchestrator.SavePaymentPlan(card.Id, checking.Id, 200m, RecurrenceFrequency.BiWeekly, next, end);
            orchestrator.SavePaymentPlan(card.Id, checking.Id, 180m, RecurrenceFrequency.BiWeekly, next.AddDays(14), end);

            var plans = transactions.GetRecurringTransferRules(card.Id).Where(rule => rule.IsPaymentPlan).ToList();
            Assert.That(plans, Has.Count.EqualTo(1));
            Assert.That(plans[0].FromAccountId, Is.EqualTo(checking.Id));
            Assert.That(plans[0].ToAccountId, Is.EqualTo(card.Id));
            Assert.That(plans[0].Amount, Is.EqualTo(180m));
            Assert.That(plans[0].Frequency, Is.EqualTo(RecurrenceFrequency.BiWeekly));
            Assert.That(plans[0].NextOccurrence.Date, Is.EqualTo(next.AddDays(14)));
            Assert.That(plans[0].EndDate, Is.EqualTo(end));
            Assert.That(orchestrator.GetPaymentPlan(card.Id)!.Id, Is.EqualTo(plans[0].Id));

            var bills = orchestrator.GetBills(checking.Id, next);
            Assert.That(bills.Select(row => row.Notes), Is.EqualTo(new[] { "Card payment" }));
            Assert.That(bills[0].Amount, Is.EqualTo(-180m));
            Assert.That(bills[0].Kind, Is.EqualTo(BillKinds.Transfer));

            orchestrator.DeletePaymentPlan(card.Id);
            Assert.That(orchestrator.GetPaymentPlan(card.Id), Is.Null);
            Assert.That(orchestrator.GetBills(checking.Id, next), Is.Empty);
        }

        [Test]
        public void GetProjection_StartsAtTodaysBalanceAndIncludesFutureBills()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var asOf = new DateTime(2026, 10, 5);
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = new DateTime(2026, 9, 8),
                DueDate = new DateTime(2026, 9, 8),
                StatementBalance = 1000m
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = new DateTime(2026, 9, 20),
                Amount = -100m,
                Description = "Groceries"
            });
            transactions.AddFutureSingleTransaction(new FutureSingleTransaction
            {
                AccountId = checking.Id,
                Date = asOf.AddDays(10),
                Amount = -50m,
                Description = "One time",
                Origin = ExpectedOrigin.Manual,
                Status = ExpectedStatus.Planned
            });
            var projection = new BillsOrchestrator(accounts, transactions, statements).GetProjection(checking.Id, asOf);

            Assert.That(projection.OpeningBalance, Is.EqualTo(900m));
            Assert.That(projection.Through, Is.EqualTo(asOf.AddMonths(1)));
            Assert.That(projection.Rows.Select(row => row.Description), Is.EqualTo(new[] { "One time" }));
            Assert.That(projection.Rows[0].Amount, Is.EqualTo(-50m));
            Assert.That(projection.Rows[0].Balance, Is.EqualTo(850m));
        }

        [Test]
        public void GetProjection_ExpandsRepeatingBillsThroughTheNextMonth()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var asOf = new DateTime(2026, 10, 5);
            checking.PostedBalance = 500m;
            accounts.UpsertAccount(checking);
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Payday bill",
                Amount = -40m,
                Frequency = RecurrenceFrequency.BiWeekly,
                NextOccurrence = asOf.AddDays(3),
                IsActive = true
            });

            var projection = new BillsOrchestrator(accounts, transactions, statements).GetProjection(checking.Id, asOf);

            Assert.That(projection.Rows.Select(row => row.Date), Is.EqualTo(new[]
            {
                new DateTime(2026, 10, 8),
                new DateTime(2026, 10, 22),
                new DateTime(2026, 11, 5)
            }));
            Assert.That(projection.Rows.Select(row => row.Balance), Is.EqualTo(new[] { 460m, 420m, 380m }));
        }

        [Test]
        public void GetProjection_ListsCreditsBeforeDebitsOnTheSameDay()
        {
            var (accounts, transactions, statements, checking, _) = SeedCheckingAndCard();
            var asOf = new DateTime(2026, 10, 5);
            var day = new DateTime(2026, 10, 12);
            checking.PostedBalance = 1000m;
            accounts.UpsertAccount(checking);
            transactions.AddFutureSingleTransaction(Bill(checking.Id, day, -80m, "Large debit"));
            transactions.AddFutureSingleTransaction(Bill(checking.Id, day, 200m, "Deposit"));
            transactions.AddFutureSingleTransaction(Bill(checking.Id, day, -50m, "Small debit"));

            var projection = new BillsOrchestrator(accounts, transactions, statements).GetProjection(checking.Id, asOf);

            Assert.That(projection.Rows.Select(row => row.Description), Is.EqualTo(new[]
            {
                "Deposit",
                "Small debit",
                "Large debit"
            }));
            Assert.That(projection.Rows.Select(row => row.Balance), Is.EqualTo(new[] { 1200m, 1150m, 1070m }));
        }

        private static FutureSingleTransaction Bill(Guid accountId, DateTime date, decimal amount, string description) =>
            new()
            {
                AccountId = accountId,
                Date = date,
                Amount = amount,
                Description = description,
                Origin = ExpectedOrigin.Manual,
                Status = ExpectedStatus.Planned
            };

        [Test]
        public void GetProjection_IncludesUnpaidStatementDueAndSkipsItAfterPay()
        {
            var (accounts, transactions, statements, checking, card) = SeedCheckingAndCard();
            var asOf = new DateTime(2026, 10, 5);
            var statement = new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = new DateTime(2026, 9, 28),
                DueDate = new DateTime(2026, 10, 20),
                StatementBalance = 1000m,
                AmountDue = 200m
            };
            statements.Save(statement);
            var orchestrator = new BillsOrchestrator(accounts, transactions, statements);

            var beforePay = orchestrator.GetProjection(card.Id, asOf);
            Assert.That(beforePay.OpeningBalance, Is.EqualTo(1000m));
            Assert.That(beforePay.Rows.Single().Description, Is.EqualTo("Statement due"));
            Assert.That(beforePay.Rows[0].Amount, Is.EqualTo(-200m));
            Assert.That(beforePay.Rows[0].Balance, Is.EqualTo(800m));

            transactions.AddFutureTransferTransaction(new FutureTransferTransaction
            {
                FromAccountId = checking.Id,
                ToAccountId = card.Id,
                Amount = 200m,
                Date = statement.DueDate,
                Description = "Card payment",
                Origin = ExpectedOrigin.StatementPay,
                OriginId = statement.Id,
                StatementId = statement.Id,
                Status = ExpectedStatus.Scheduled
            });

            var afterPay = orchestrator.GetProjection(card.Id, asOf);
            Assert.That(afterPay.Rows.Select(row => row.Description), Is.EqualTo(new[] { "Card payment" }));
            Assert.That(afterPay.Rows[0].Amount, Is.EqualTo(-200m));
            Assert.That(afterPay.Rows[0].Balance, Is.EqualTo(800m));
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
