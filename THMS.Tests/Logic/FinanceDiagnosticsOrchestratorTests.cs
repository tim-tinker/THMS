using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class FinanceDiagnosticsOrchestratorTests
    {
        [Test]
        public void TransactionDiagnostics_ReportsUncategorizedDuplicatesOrphansAndZeroAmounts()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);

            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today,
                Description = "Coffee",
                Amount = -4
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today,
                Description = "Coffee",
                Amount = -4
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today,
                Description = "",
                Amount = 0
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(3),
                Description = "Ghost",
                Amount = -10,
                CategoryId = DefaultExpenseCategories.GroceriesId,
                Category = DefaultExpenseCategories.Groceries
            });

            var findings = new TransactionDiagnosticsOrchestrator(accounts, transactions).Run();

            Assert.That(findings, Has.Some.Contains("Uncategorized transaction Coffee"));
            Assert.That(findings, Has.Some.Contains("Duplicate transaction Coffee"));
            Assert.That(findings, Has.Some.Contains("Zero-amount transaction"));
            Assert.That(findings, Has.Some.Contains("Missing description"));
            Assert.That(findings, Has.Some.Contains("Posted transaction is in the future: Ghost"));
            Assert.That(findings, Has.Some.Contains("Orphaned transaction Ghost"));
        }

        [Test]
        public void SplitDiagnostics_ReportsMismatchMissingDestinationAndUncategorizedRows()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);

            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today,
                Description = "HEB",
                Amount = -80,
                Splits =
                [
                    new() { Amount = -50, Type = SplitType.Expense, CategoryId = DefaultExpenseCategories.GroceriesId, Category = DefaultExpenseCategories.Groceries },
                    new() { Amount = -20, Type = SplitType.Expense }
                ]
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today,
                Description = "Payroll",
                Amount = 1000,
                Splits =
                [
                    new() { Amount = 800, Type = SplitType.Income, CategoryId = DefaultExpenseCategories.UncategorizedId, Category = DefaultExpenseCategories.Uncategorized },
                    new() { Amount = 200, Type = SplitType.Transfer }
                ]
            });

            var findings = new SplitDiagnosticsOrchestrator(accounts, transactions).Run();

            Assert.That(findings, Has.Some.Contains("Split sum mismatch HEB"));
            Assert.That(findings, Has.Some.Contains("Uncategorized expense split: HEB"));
            Assert.That(findings, Has.Some.Contains("Transfer split missing destination: Payroll"));
        }

        [Test]
        public void LoanDiagnostics_ReportsMissingMetadataUnsplitPaymentsAndNegativePrincipal()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var auto = new LoanAccount { Name = "Auto", Institution = "X", AccountNumber = "2", WebsiteUrl = "" };
            var mortgage = new MortgageAccount
            {
                Name = "House",
                Institution = "X",
                AccountNumber = "3",
                WebsiteUrl = "",
                Principal = 1000,
                InterestRate = 0.05m,
                TermMonths = 12,
                NextPaymentDate = DateTime.Today.AddDays(5)
            };
            accounts.UpsertAccount(auto);
            accounts.UpsertAccount(mortgage);
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = mortgage.Id,
                Date = DateTime.Today,
                Description = "Mortgage payment",
                Amount = -2000,
                Splits =
                [
                    new() { Amount = -1500, Type = SplitType.Principal, CategoryId = DefaultExpenseCategories.PaymentId, Category = DefaultExpenseCategories.Payment },
                    new() { Amount = -500, Type = SplitType.Interest, CategoryId = DefaultExpenseCategories.PaymentId, Category = DefaultExpenseCategories.Payment }
                ]
            });

            var findings = new LoanDiagnosticsOrchestrator(accounts, transactions).Run();

            Assert.That(findings, Has.Some.Contains("Missing loan metadata"));
            Assert.That(findings, Has.Some.Contains("Loan with no payments: Auto"));
            Assert.That(findings, Has.Some.Contains("Remaining principal is negative: House"));
        }

        [Test]
        public void ForecastDiagnostics_ReportsStaleEndedAndMissingAccounts()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);

            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Rent",
                Amount = -1200,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddMonths(-2),
                EndDate = DateTime.Today.AddMonths(-1),
                IsActive = true
            });
            transactions.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = Guid.NewGuid(),
                ToAccountId = Guid.NewGuid(),
                Description = "Ghost transfer",
                Amount = 100,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(1),
                IsActive = true
            });
            var savings = new BankAccount { Name = "Savings", Institution = "X", AccountNumber = "2", WebsiteUrl = "" };
            accounts.UpsertAccount(savings);
            transactions.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = savings.Id,
                ToAccountId = savings.Id,
                Description = "Loop",
                Amount = 50,
                Frequency = RecurrenceFrequency.Weekly,
                NextOccurrence = DateTime.Today.AddDays(3),
                IsActive = true
            });

            var findings = new ForecastDiagnosticsOrchestrator(accounts, transactions).Run();

            Assert.That(findings, Has.Some.Contains("Stale next occurrence: Rent"));
            Assert.That(findings, Has.Some.Contains("Active rule already ended: Rent"));
            Assert.That(findings, Has.Some.Contains("Recurring transfer account missing: Ghost transfer"));
            Assert.That(findings, Has.Some.Contains("Recurring transfer from and to are the same: Loop"));
            Assert.That(findings, Has.Some.Contains("Active recurring rules produce no forecast in the next year: Checking"));
        }

        [Test]
        public void Diagnostics_ReportNoIssuesWhenLedgerIsClean()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today.AddDays(-1),
                Description = "Paycheck",
                Amount = 100,
                CategoryId = DefaultExpenseCategories.GroceriesId,
                Category = DefaultExpenseCategories.Groceries
            });

            Assert.That(new TransactionDiagnosticsOrchestrator(accounts, transactions).Run(), Is.EqualTo(new[] { "No issues found." }));
            Assert.That(new SplitDiagnosticsOrchestrator(accounts, transactions).Run(), Is.EqualTo(new[] { "No issues found." }));
            Assert.That(new LoanDiagnosticsOrchestrator(accounts, transactions).Run(), Is.EqualTo(new[] { "No issues found." }));
            Assert.That(new ForecastDiagnosticsOrchestrator(accounts, transactions).Run(), Is.EqualTo(new[] { "No issues found." }));
        }
    }
}
