using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

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
                StatementBalance = 300,
                Promotions =
                [
                    new() { AccountId = cardWithStatement.Id, Amount = 50, Deadline = DateTime.Today.AddDays(5), Type = PromoType.LumpSum }
                ]
            });

            var rows = new PlanningOrchestrator(accounts, transactions, statements).GetUpcomingObligations(DateTime.Today);

            Assert.That(rows, Has.Some.Matches<THMS.Logic.ViewModels.Finance.UpcomingObligation>(o =>
                o.AccountName == "Store Card" && o.AmountDue == 300 && o.PromotionalDue == 50));
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
                StatementBalance = 1090.73m
            });

            var rows = new PlanningOrchestrator(accounts, transactions, statements).GetUpcomingObligations(DateTime.Today);

            Assert.That(rows, Is.Empty);
        }

        [Test]
        public void GetUpcomingObligations_IncludesRecurringOutgoingForecasts()
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
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Rent",
                Amount = -1850,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(5),
                IsActive = true
            });
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = checking.Id,
                Description = "Paycheck",
                Amount = 2400,
                Frequency = RecurrenceFrequency.BiWeekly,
                NextOccurrence = DateTime.Today.AddDays(2),
                IsActive = true
            });

            var rows = new PlanningOrchestrator(accounts, transactions, statements).GetUpcomingObligations(DateTime.Today);

            Assert.That(rows, Has.Some.Matches<THMS.Logic.ViewModels.Finance.UpcomingObligation>(o =>
                o.AccountName == "Checking" && o.AmountDue == 1850 && o.Notes == "Rent"));
            Assert.That(rows, Has.None.Matches<THMS.Logic.ViewModels.Finance.UpcomingObligation>(o =>
                o.Notes.Contains("Paycheck")));
        }

        [Test]
        public void GetUpcomingObligations_UsesRecurringTransferToCreditWhenNoStatement()
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
            var card = new CreditAccount
            {
                Name = "Card",
                Institution = "X",
                AccountNumber = "2",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(card);
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

            var rows = new PlanningOrchestrator(accounts, transactions, statements).GetUpcomingObligations(DateTime.Today);

            Assert.That(rows, Has.Some.Matches<THMS.Logic.ViewModels.Finance.UpcomingObligation>(o =>
                o.AccountName == "Card" && o.AmountDue == 125 && o.Notes == "Card payment"));
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
                StatementBalance = 100
            });

            var all = new PlanningOrchestrator(accounts, transactions, statements).GetAllStatements();

            Assert.That(all, Has.Count.EqualTo(1));
            Assert.That(all[0], Is.TypeOf<BankStatement>());
        }

        [Test]
        public void GetStatementListRows_MapsAmountDueWithoutInterest()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = new DateTime(2026, 8, 31),
                DueDate = new DateTime(2026, 8, 31),
                StatementBalance = 101.25m
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = new DateTime(2026, 8, 15),
                Amount = 1.25m,
                CategoryId = DefaultExpenseCategories.InterestId,
                Category = DefaultExpenseCategories.Interest
            });

            var rows = new PlanningOrchestrator(accounts, transactions, statements).GetStatementListRows(checking.Id);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].AmountDue, Is.EqualTo(AccountStatementListRow.NotApplicable));
            Assert.That(rows[0].StatementBalance, Is.EqualTo(101.25m.ToString("c2")));
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
                StatementBalance = 100
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
        public void GeneratePayAllDue_CreatesUnrealizedPlannedTransactions()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            var created = orchestrator.GeneratePayAllDue(DateTime.Today.AddDays(30));

            Assert.That(created, Has.Count.EqualTo(1));
            Assert.That(created[0].IsPlannedPayment, Is.True);
            Assert.That(created[0].Amount, Is.EqualTo(220m));
            Assert.That(created[0].IsRealized, Is.False);
            Assert.That(orchestrator.GeneratePayAllDue(DateTime.Today.AddDays(30)), Is.Empty);
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
                    StatementBalance = 50
                }),
                Throws.InvalidOperationException.With.Message.Contains("Due date"));
        }

        [Test]
        public void ReconcileManualPayment_PostsLoanPaymentWithoutSplits()
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
                StatementBalance = 8000
            };
            statements.Save(statement);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            var planned = orchestrator.AddPlannedPayment(loan.Id, 200, DateTime.Today.AddDays(5), "Loan payment", statement.Id);

            orchestrator.ReconcileManualPayment(planned.Id, DateTime.Today);

            var posted = transactions.GetPostedTransactions(loan.Id).Single();
            Assert.That(posted.Amount, Is.EqualTo(-200m));
            Assert.That(posted.HasSplits, Is.False);
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
