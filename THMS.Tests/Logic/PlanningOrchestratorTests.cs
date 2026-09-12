using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class PlanningOrchestratorTests
    {
        [Test]
        public void GetUpcomingObligations_UsesStatementsAndAccountMetadata()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var card = new CreditAccount
            {
                Name = "Card",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = "",
                PostedBalance = -400,
                DueDate = DateTime.Today.AddDays(-3)
            };
            var cardWithStatement = new CreditAccount
            {
                Name = "Store Card",
                Institution = "X",
                AccountNumber = "2",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(card);
            accounts.UpsertAccount(cardWithStatement);
            statements.Save(new CreditCardStatement
            {
                AccountId = cardWithStatement.Id,
                StatementDate = DateTime.Today.AddDays(-20),
                DueDate = DateTime.Today.AddDays(10),
                AmountDue = 300,
                MinimumPayment = 35,
                StatementBalance = 300,
                Promotions =
                [
                    new() { AccountId = cardWithStatement.Id, Amount = 50, Deadline = DateTime.Today.AddDays(5), Type = PromoType.LumpSum }
                ]
            });

            var rows = new PlanningOrchestrator(accounts, transactions, statements).GetUpcomingObligations(DateTime.Today);

            Assert.That(rows, Has.Some.Matches<THMS.Logic.ViewModels.Finance.UpcomingObligation>(o =>
                o.AccountName == "Store Card" && o.MinimumPayment == 35 && o.PromotionalDue == 50));
            Assert.That(rows, Has.Some.Matches<THMS.Logic.ViewModels.Finance.UpcomingObligation>(o =>
                o.AccountName == "Card" && o.AmountDue == 400 && o.Notes.Contains("account metadata")));
        }

        [Test]
        public void GetUpcomingObligations_DoesNotTreatBankStatementsAsPayments()
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
                PostedBalance = 1090.73m
            };
            accounts.UpsertAccount(checking);
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                PeriodStart = DateTime.Today.AddDays(-30),
                BeginningBalance = 3777.56m,
                Deposits = 16859.24m,
                Withdrawals = 19546.07m,
                InterestEarned = 1.25m,
                EndingBalance = 1090.73m
            });

            var rows = new PlanningOrchestrator(accounts, transactions, statements).GetUpcomingObligations(DateTime.Today);

            Assert.That(rows, Is.Empty);
        }

        [Test]
        public void GetAllStatements_IncludesBankStatements()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                BeginningBalance = 100,
                EndingBalance = 100
            });

            var all = new PlanningOrchestrator(accounts, transactions, statements).GetAllStatements();

            Assert.That(all, Has.Count.EqualTo(1));
            Assert.That(all[0], Is.TypeOf<BankStatement>());
        }

        [Test]
        public void EnsureStatementPayment_CreatesExpenseOnFundingAccountForUntrackedBiller()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            var electric = new UntrackedAccount
            {
                Name = "Electric",
                Institution = "Duke",
                AccountNumber = "A1",
                Type = AccountType.Utility
            };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(electric);
            var statement = new UtilityStatement
            {
                AccountId = electric.Id,
                StatementDate = DateTime.Today.AddDays(-10),
                DueDate = DateTime.Today.AddDays(8),
                AmountDue = 80,
                MinimumPayment = 80,
                Charges = [new() { Description = "Energy", Amount = 80 }]
            };
            statements.Save(statement);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            orchestrator.EnsureStatementPayment(statement, checking.Id);

            var planned = transactions.GetAllPlannedPayments().Single();
            Assert.That(planned.AccountId, Is.EqualTo(checking.Id));
            Assert.That(planned.Amount, Is.EqualTo(-80m));
            Assert.That(planned.Date.Date, Is.EqualTo(statement.DueDate.Date));
            Assert.That(planned.StatementId, Is.EqualTo(statement.Id));
            Assert.That(transactions.GetAllPlannedTransfers(), Is.Empty);
        }

        [Test]
        public void EnsureStatementPayment_CreatesTransferFromFundingAccountToTrackedDebt()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "9", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            var statement = statements.GetForAccount(card.Id).Single();

            orchestrator.EnsureStatementPayment(statement, checking.Id);

            var transfer = transactions.GetAllPlannedTransfers().Single();
            Assert.That(transfer.FromAccountId, Is.EqualTo(checking.Id));
            Assert.That(transfer.ToAccountId, Is.EqualTo(card.Id));
            Assert.That(transfer.Amount, Is.EqualTo(220m));
            Assert.That(transfer.Date.Date, Is.EqualTo(statement.DueDate.Date));
            Assert.That(orchestrator.GetPlannedPaymentViews().Single().AccountName, Does.Contain("Checking").And.Contain("Card"));
            Assert.That(orchestrator.GeneratePayAllDue(DateTime.Today.AddDays(30)), Is.Empty);
        }

        [Test]
        public void EnsureStatementPayment_BankStatementDoesNotCreateAPayment()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            var statement = new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                BeginningBalance = 100,
                EndingBalance = 100
            };
            statements.Save(statement);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            orchestrator.EnsureStatementPayment(statement, checking.Id);

            Assert.That(transactions.GetAllPlannedPayments(), Is.Empty);
            Assert.That(transactions.GetAllPlannedTransfers(), Is.Empty);
        }

        [Test]
        public void DeleteStatement_RemovesLinkedPlannedPayment()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            var electric = new UntrackedAccount
            {
                Name = "Electric",
                Institution = "Duke",
                AccountNumber = "A1",
                Type = AccountType.Utility
            };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(electric);
            var statement = new UtilityStatement
            {
                AccountId = electric.Id,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(5),
                AmountDue = 40,
                MinimumPayment = 40,
                Charges = [new() { Description = "Energy", Amount = 40 }]
            };
            statements.Save(statement);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            orchestrator.EnsureStatementPayment(statement, checking.Id);

            orchestrator.DeleteStatement(statement.Id);

            Assert.That(transactions.GetAllPlannedPayments(), Is.Empty);
            Assert.That(statements.Get(statement.Id), Is.Null);
        }

        [Test]
        public void GenerateMinimumPayments_CreatesUnrealizedPlannedTransactions()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            var created = orchestrator.GenerateMinimumPayments(DateTime.Today.AddDays(30));

            Assert.That(created, Has.Count.EqualTo(1));
            Assert.That(created[0].IsPlannedPayment, Is.True);
            Assert.That(created[0].Amount, Is.EqualTo(35m));
            Assert.That(created[0].IsRealized, Is.False);
            Assert.That(orchestrator.GenerateMinimumPayments(DateTime.Today.AddDays(30)), Is.Empty);
        }

        [Test]
        public void GeneratePromotionAndPayAllDue_CreateDistinctPlannedPayments()
        {
            var (accounts, transactions, statements, _) = SeedCardStatement();
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            var promos = orchestrator.GeneratePromotionPayments(DateTime.Today.AddDays(30));
            var payAll = orchestrator.GeneratePayAllDue(DateTime.Today.AddDays(30));

            Assert.That(promos, Has.Count.EqualTo(1));
            Assert.That(promos[0].PlanningNote, Does.Contain("Promotion"));
            Assert.That(payAll, Has.Count.EqualTo(1));
            Assert.That(payAll[0].PlanningNote, Is.EqualTo("Full Payment"));
        }

        [Test]
        public void CommitPlannedPayments_PostsWhenDue_LeavesFutureDated()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "", PostedBalance = 1000 };
            accounts.UpsertAccount(checking);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            var due = orchestrator.AddPlannedPayment(checking.Id, 40, DateTime.Today, "Due now");
            var later = orchestrator.AddPlannedPayment(checking.Id, 40, DateTime.Today.AddDays(10), "Later");

            var committed = orchestrator.CommitPlannedPayments([due, later]);

            Assert.That(committed, Is.EqualTo(1));
            Assert.That(transactions.GetPostedTransactions(checking.Id).Count(), Is.EqualTo(1));
            Assert.That(transactions.GetFutureSingleTransaction(due.Id), Is.Null);
            Assert.That(transactions.GetFutureSingleTransaction(later.Id)!.IsRealized, Is.False);
        }

        [Test]
        public void ReconcileManualPayment_CreatesPostedTransaction()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            var planned = orchestrator.AddPlannedPayment(checking.Id, 75, DateTime.Today.AddDays(3), "Manual");

            var count = orchestrator.ReconcileManualPayment(planned.Id, DateTime.Today);

            Assert.That(count, Is.EqualTo(1));
            var posted = transactions.GetPostedTransactions(checking.Id).Single();
            Assert.That(posted.Amount, Is.EqualTo(-75m));
            Assert.That(posted.Date.Date, Is.EqualTo(DateTime.Today));
            Assert.That(transactions.GetFutureSingleTransaction(planned.Id), Is.Null);
        }

        [Test]
        public void ComputeCashFlow_AppliesBankIncomeAndPlannedOutflows()
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
                PostedBalance = 1000
            };
            accounts.UpsertAccount(checking);
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Amount = 500,
                Description = "Paycheck",
                Frequency = RecurrenceFrequency.BiWeekly,
                NextOccurrence = DateTime.Today.AddDays(2),
                IsActive = true
            });
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            orchestrator.AddPlannedPayment(checking.Id, 100, DateTime.Today.AddDays(3), "Bill");

            var forecast = orchestrator.ComputeCashFlow(DateTime.Today.AddDays(20));

            Assert.That(forecast.CurrentBalance, Is.EqualTo(1000m));
            Assert.That(forecast.ForecastedBalanceNextPayday, Is.EqualTo(1500m));
            Assert.That(forecast.ForecastedBalanceAfterPlanned, Is.EqualTo(1900m));
        }

        [Test]
        public void GenerateExtraPrincipal_CreatesPaymentForEachLoanAndMortgage()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var auto = new LoanAccount
            {
                Name = "Auto",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = "",
                Principal = 8000,
                InterestRate = 0.09m,
                TermMonths = 36
            };
            var house = new MortgageAccount
            {
                Name = "House",
                Institution = "X",
                AccountNumber = "2",
                WebsiteUrl = "",
                Principal = 200000,
                InterestRate = 0.04m,
                TermMonths = 360,
                NextPaymentDate = DateTime.Today.AddDays(8)
            };
            accounts.UpsertAccount(auto);
            accounts.UpsertAccount(house);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            var created = orchestrator.GenerateExtraPrincipalPayments(200);

            Assert.That(created, Has.Count.EqualTo(2));
            Assert.That(created.Select(p => p.AccountId), Is.EquivalentTo(new[] { auto.Id, house.Id }));
            Assert.That(created.Select(p => p.Amount), Has.All.EqualTo(-200m));
            Assert.That(
                () => orchestrator.GenerateExtraPrincipalPayments(0),
                Throws.InvalidOperationException.With.Message.Contains("greater than zero"));
        }

        [Test]
        public void SaveStatement_RejectsInvalidDates()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            Assert.That(
                () => orchestrator.SaveStatement(new CreditCardStatement
                {
                    AccountId = card.Id,
                    StatementDate = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(-1),
                    AmountDue = 50,
                    MinimumPayment = 25,
                    StatementBalance = 50
                }),
                Throws.InvalidOperationException.With.Message.Contains("Due date"));
        }

        [Test]
        public void ReconcileManualPayment_SplitsLoanInterestAndPrincipal()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var loan = new LoanAccount
            {
                Name = "Auto",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = "",
                Principal = 8000,
                InterestRate = 0.09m,
                TermMonths = 36
            };
            accounts.UpsertAccount(loan);
            var statement = new LoanStatement
            {
                AccountId = loan.Id,
                StatementDate = DateTime.Today.AddDays(-20),
                DueDate = DateTime.Today.AddDays(5),
                AmountDue = 200,
                MinimumPayment = 200,
                PrincipalBalance = 8000,
                InterestCharged = 40
            };
            statements.Save(statement);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            var planned = orchestrator.AddPlannedPayment(loan.Id, 200, DateTime.Today.AddDays(5), "Loan payment", statement.Id);

            orchestrator.ReconcileManualPayment(planned.Id, DateTime.Today);

            var posted = transactions.GetPostedTransactions(loan.Id).Single();
            Assert.That(posted.HasSplits, Is.True);
            Assert.That(posted.Splits.Single(s => s.Type == SplitType.Principal).Amount, Is.EqualTo(-160m));
            Assert.That(posted.Splits.Single(s => s.Type == SplitType.Interest).Amount, Is.EqualTo(-40m));
        }

        [Test]
        public void ComputeCashFlow_IncludesUtilityUsageForecast()
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
                PostedBalance = 1000
            };
            accounts.UpsertAccount(checking);
            statements.Save(new UtilityStatement
            {
                AccountId = checking.Id,
                StatementDate = DateTime.Today.AddDays(-10),
                DueDate = DateTime.Today.AddDays(4),
                AmountDue = 80,
                MinimumPayment = 80,
                Usage = [new() { Type = "kWh", Amount = 400, Rate = 0.20m }],
                Charges = [new() { Description = "Energy", Amount = 80 }]
            });

            var forecast = new PlanningOrchestrator(accounts, transactions, statements)
                .ComputeCashFlow(DateTime.Today.AddDays(20));

            Assert.That(forecast.ForecastedBalanceAfterPlanned, Is.EqualTo(920m));
        }

        private static (InMemoryAccountDataStore accounts, InMemoryTransactionDataStore transactions, InMemoryAccountStatementDataStore statements, CreditAccount card) SeedCardStatement()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var card = new CreditAccount
            {
                Name = "Card",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(card);
            statements.Save(new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-15),
                DueDate = DateTime.Today.AddDays(7),
                AmountDue = 220,
                MinimumPayment = 35,
                StatementBalance = 220,
                Promotions =
                [
                    new()
                    {
                        AccountId = card.Id,
                        Amount = 80,
                        Deadline = DateTime.Today.AddDays(5),
                        Type = PromoType.LumpSum
                    }
                ]
            });
            return (accounts, transactions, statements, card);
        }
    }
}
