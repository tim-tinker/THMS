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

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(DateTime.Today);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Kind, Is.EqualTo(BillKinds.Statement));
            Assert.That(rows[0].Amount, Is.EqualTo(220m));
            Assert.That(rows[0].DestinationAccountId, Is.EqualTo(card.Id));
            Assert.That(rows[0].FundingAccountId, Is.EqualTo(Guid.Empty));
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

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(DateTime.Today);

            Assert.That(rows, Has.Some.Matches<BillRow>(r => r.Notes == "Comcast" && r.Kind == BillKinds.Recurring && r.Amount == 80m));
            Assert.That(rows, Has.Some.Matches<BillRow>(r => r.Notes == "HSA Transfer" && r.Kind == BillKinds.Transfer && r.Amount == 448m));
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
            var rows = orchestrator.GetBills(DateTime.Today);
            rows[0].Pay = true;

            Assert.That(orchestrator.CashRemaining(), Is.EqualTo(1000m));
            Assert.That(orchestrator.CashRemaining(rows), Is.EqualTo(-850m));
            var scheduled = orchestrator.Schedule(rows);
            Assert.That(scheduled, Has.Count.EqualTo(1));
            Assert.That(transactions.GetPostedTransactions(checking.Id), Is.Empty);
            Assert.That(orchestrator.CashRemaining(), Is.EqualTo(-850m));
            Assert.That(orchestrator.GetBills(DateTime.Today).Single().Status, Is.EqualTo(BillStatuses.Scheduled));
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
            var row = orchestrator.GetBills(DateTime.Today).Single();
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
            Assert.That(transactions.GetScheduledPaymentIntents(), Is.Empty);
            var updated = transactions.GetRecurringSingleRule(rule.Id)!;
            Assert.That(updated.LastOccurrence, Is.EqualTo(due.AddDays(1)));
            Assert.That(updated.NextOccurrence, Is.EqualTo(due.AddDays(1).AddMonths(1)));
            Assert.That(orchestrator.GetBills(DateTime.Today).Any(r => r.Notes == "Spotify" && r.Status == BillStatuses.Scheduled), Is.False);
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
            var row = orchestrator.GetBills(DateTime.Today).Single();
            row.Pay = true;
            var intent = orchestrator.Schedule([row]).Single();
            orchestrator.Unschedule(intent.Id);

            var bills = orchestrator.GetBills(DateTime.Today);
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

            var rows = new BillsOrchestrator(accounts, transactions, statements).GetBills(DateTime.Today);

            Assert.That(rows, Has.Some.Matches<BillRow>(r => r.Notes == "Overdue donation" && r.Amount == 25m));
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
            var row = orchestrator.GetBills(DateTime.Today).Single();
            row.Pay = true;
            orchestrator.Schedule([row]);

            var forecast = new TransactionOrchestrator(transactions).GenerateForecast(
                checking.Id, DateTime.Today, DateTime.Today.AddDays(40));

            Assert.That(forecast.Any(v => v.Date.Date == due.Date && Math.Abs(v.Amount) == 40m), Is.False);
            Assert.That(forecast.Any(v => v.Date.Date == due.AddMonths(1).Date && Math.Abs(v.Amount) == 40m), Is.True);
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
