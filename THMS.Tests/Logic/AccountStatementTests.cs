using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class AccountStatementTests
    {
        [Test]
        public void InMemoryStore_RoundTripsEachStatementType()
        {
            AssertRoundTrip(new InMemoryAccountStatementDataStore());
        }

        [Test]
        public void SqliteStore_RoundTripsEachStatementType()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-statements-{Guid.NewGuid():N}.db");
            try
            {
                AssertRoundTrip(new SQLiteAccountStatementDataStore(path));
            }
            finally
            {
                TryDelete(path);
            }
        }

        [Test]
        public void Validator_EnforcesUniversalAndTypeRules()
        {
            Assert.That(AccountStatementValidator.Validate(new BankStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                PeriodStart = DateTime.Today.AddDays(1),
                BeginningBalance = 100,
                Deposits = 10,
                Withdrawals = 5,
                EndingBalance = 90
            }), Has.Some.Contains("Period start").And.Some.Contains("Ending balance"));

            Assert.That(AccountStatementValidator.Validate(new BankStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                BeginningBalance = -1,
                EndingBalance = 0
            }), Has.Some.Contains("Beginning balance"));

            Assert.That(AccountStatementValidator.Validate(new LoanStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(-1),
                AmountDue = 10,
                MinimumPayment = 20,
                PrincipalBalance = -1
            }), Has.Some.Contains("Due date").And.Some.Contains("minimum payment").And.Some.Contains("principal"));

            Assert.That(AccountStatementValidator.Validate(new MortgageStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 100,
                MinimumPayment = 100,
                EscrowBalance = -5
            }), Has.Some.Contains("escrow"));

            Assert.That(AccountStatementValidator.Validate(new CreditCardStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 50,
                MinimumPayment = 25,
                StatementBalance = -1,
                Promotions = [new() { Amount = 10, Deadline = default }]
            }), Has.Some.Contains("statement balance").And.Some.Contains("deadline"));

            Assert.That(AccountStatementValidator.Validate(new UtilityStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 50,
                MinimumPayment = 50,
                Usage = [new() { Type = "kWh", Amount = 0 }],
                Charges = [new() { Description = "Energy", Amount = 40 }]
            }), Has.Some.Contains("positive amount").And.Some.Contains("must equal amount due"));

            Assert.That(AccountStatementValidator.Validate(new ServiceStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 15,
                MinimumPayment = 15,
                Charges = [new() { Description = "Base", Amount = 10 }]
            }), Has.Some.Contains("must equal amount due"));

            Assert.That(AccountStatementValidator.Validate(new InsuranceStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 100,
                MinimumPayment = 100,
                Premium = 80,
                Fees = 10
            }), Has.Some.Contains("must equal amount due"));

            Assert.That(AccountStatementValidator.Validate(ValidUtility()), Is.Empty);
            Assert.That(AccountStatementValidator.Validate(new BankStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                PeriodStart = DateTime.Today.AddDays(-30),
                BeginningBalance = 100,
                Deposits = 50,
                Withdrawals = 20,
                InterestEarned = 1.25m,
                EndingBalance = 131.25m
            }), Is.Empty);
            Assert.That(AccountStatementValidator.Validate(new BankStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                BeginningBalance = 100,
                Deposits = 50,
                Withdrawals = 20,
                Fees = 4,
                EndingBalance = 126
            }), Is.Empty);
            Assert.That(AccountStatementValidator.Validate(new BankStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                BeginningBalance = 100,
                Deposits = 50,
                Withdrawals = 20,
                InterestEarned = 1.25m,
                Fees = 4,
                EndingBalance = 130
            }), Has.Some.Contains("Ending balance"));
            Assert.That(AccountStatementValidator.Validate(new BankStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                BeginningBalance = 1090.73m,
                EndingBalance = 1090.73m
            }), Is.Empty);
        }

        [Test]
        public void Diagnostics_ReportsMissingAndMismatchedStatements()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new AccountStatementDataStore();
            var card = new CreditAccount
            {
                Name = "Card",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = "",
                APR = 0.20m,
                CreditLimit = 5000,
                PostedBalance = -400
            };
            var loan = new LoanAccount
            {
                Name = "Auto",
                Institution = "X",
                AccountNumber = "2",
                WebsiteUrl = "",
                Principal = 8000,
                TermMonths = 36
            };
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "X",
                AccountNumber = "3",
                WebsiteUrl = "",
                PostedBalance = 500
            };
            accounts.UpsertAccount(card);
            accounts.UpsertAccount(loan);
            accounts.UpsertAccount(checking);
            statements.Save(new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-20),
                DueDate = DateTime.Today.AddDays(10),
                AmountDue = 350,
                MinimumPayment = 35,
                StatementBalance = 350
            });
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                PeriodStart = DateTime.Today.AddDays(-30),
                BeginningBalance = 400,
                Deposits = 200,
                Withdrawals = 0,
                EndingBalance = 600
            });

            var findings = new AccountDiagnosticsOrchestrator(accounts, transactions, statements).Run();

            Assert.That(findings, Has.Some.Contains("Missing statement: Auto"));
            Assert.That(findings, Has.Some.Contains("Mismatched statement balance for Card"));
            Assert.That(findings, Has.Some.Contains("Mismatched statement balance for Checking"));
        }

        private static UtilityStatement ValidUtility() => new()
        {
            StatementDate = DateTime.Today,
            DueDate = DateTime.Today,
            AmountDue = 80,
            MinimumPayment = 80,
            Usage = [new() { Type = "kWh", Amount = 400, Rate = 0.20m }],
            Charges = [new() { Description = "Energy", Amount = 80 }]
        };

        private static void AssertRoundTrip(IAccountStatementDataStore store)
        {
            var accountId = Guid.NewGuid();
            AccountStatement[] statements =
            [
                new BankStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today,
                    DueDate = DateTime.Today,
                    PeriodStart = DateTime.Today.AddDays(-30),
                    BeginningBalance = 3777.56m,
                    Deposits = 16859.24m,
                    Withdrawals = 19546.07m,
                    InterestEarned = 1.25m,
                    Fees = 0,
                    EndingBalance = 1090.73m
                },
                new LoanStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-20),
                    DueDate = DateTime.Today.AddDays(5),
                    AmountDue = 200,
                    MinimumPayment = 200,
                    PrincipalBalance = 5000,
                    InterestCharged = 22.5m
                },
                new MortgageStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-20),
                    DueDate = DateTime.Today.AddDays(6),
                    AmountDue = 1800,
                    MinimumPayment = 1800,
                    PrincipalBalance = 200000,
                    EscrowBalance = 1200
                },
                new CreditCardStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-15),
                    DueDate = DateTime.Today.AddDays(10),
                    AmountDue = 300,
                    MinimumPayment = 35,
                    StatementBalance = 300,
                    Promotions = [new() { AccountId = accountId, Amount = 50, Deadline = DateTime.Today.AddDays(8), Type = PromoType.LumpSum }]
                },
                new UtilityStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-10),
                    DueDate = DateTime.Today.AddDays(12),
                    AmountDue = 80,
                    MinimumPayment = 80,
                    Usage = [new() { Type = "kWh", Amount = 400, Rate = 0.20m }],
                    Charges = [new() { Description = "Energy", Amount = 80 }]
                },
                new ServiceStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-5),
                    DueDate = DateTime.Today.AddDays(15),
                    AmountDue = 15,
                    MinimumPayment = 15,
                    Charges = [new() { Description = "Streaming", Amount = 15 }]
                },
                new InsuranceStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-8),
                    DueDate = DateTime.Today.AddDays(20),
                    AmountDue = 110,
                    MinimumPayment = 110,
                    Premium = 100,
                    Fees = 10
                }
            ];

            foreach (var statement in statements)
                store.Save(statement);

            Assert.That(store.GetForAccount(accountId), Has.Count.EqualTo(7));
            Assert.That(store.GetUpcoming(DateTime.Today), Has.Count.EqualTo(7));
            var bank = (BankStatement)store.Get(statements[0].Id)!;
            Assert.That(bank.EndingBalance, Is.EqualTo(1090.73m));
            Assert.That(bank.InterestEarned, Is.EqualTo(1.25m));
            Assert.That(((LoanStatement)store.Get(statements[1].Id)!).PrincipalBalance, Is.EqualTo(5000m));
            Assert.That(((MortgageStatement)store.Get(statements[2].Id)!).EscrowBalance, Is.EqualTo(1200m));
            var card = (CreditCardStatement)store.Get(statements[3].Id)!;
            Assert.That(card.Promotions, Has.Count.EqualTo(1));
            Assert.That(card.Promotions[0].Amount, Is.EqualTo(50m));
            var utility = (UtilityStatement)store.Get(statements[4].Id)!;
            Assert.That(utility.Usage[0].Amount, Is.EqualTo(400m));
            Assert.That(utility.Charges[0].Amount, Is.EqualTo(80m));
            Assert.That(((ServiceStatement)store.Get(statements[5].Id)!).Charges[0].Description, Is.EqualTo("Streaming"));
            Assert.That(((InsuranceStatement)store.Get(statements[6].Id)!).Premium, Is.EqualTo(100m));

            store.Delete(statements[2].Id);
            Assert.That(store.Get(statements[2].Id), Is.Null);
            Assert.That(store.GetForAccount(accountId), Has.Count.EqualTo(6));
        }

        [Test]
        public void StatementAccountMatch_FiltersTrackedAndUntrackedAccounts()
        {
            var bank = new BankAccount { Name = "Checking" };
            var utility = new UntrackedAccount { Type = AccountType.Utility };
            var service = new UntrackedAccount { Type = AccountType.Service };
            var insurance = new UntrackedAccount { Type = AccountType.Insurance };

            Assert.That(StatementAccountMatch.Matches(bank, StatementType.Bank), Is.True);
            Assert.That(StatementAccountMatch.Matches(bank, StatementType.Utility), Is.False);
            Assert.That(StatementAccountMatch.Matches(utility, StatementType.Utility), Is.True);
            Assert.That(StatementAccountMatch.Matches(utility, StatementType.Bank), Is.False);
            Assert.That(StatementAccountMatch.Matches(service, StatementType.Service), Is.True);
            Assert.That(StatementAccountMatch.Matches(insurance, StatementType.Insurance), Is.True);
            Assert.That(StatementAccountMatch.ForAccount(bank), Is.EqualTo(StatementType.Bank));
            Assert.That(StatementAccountMatch.ForAccount(utility), Is.EqualTo(StatementType.Utility));
            Assert.That(StatementAccountMatch.CreateAccount(StatementType.Utility), Is.TypeOf<UntrackedAccount>());
            Assert.That(StatementAccountMatch.CreateAccount(StatementType.Utility).Type, Is.EqualTo(AccountType.Utility));
        }

        [Test]
        public void SqliteAccountStore_RoundTripsUntrackedAccounts()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-untracked-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteAccountDataStore(path);
                var electric = new UntrackedAccount
                {
                    Name = "Home Electric",
                    Institution = "Duke Energy",
                    AccountNumber = "ABC-123",
                    Type = AccountType.Utility,
                    WebsiteUrl = "https://duke.example"
                };
                store.UpsertAccount(electric);

                var loaded = store.GetAccount("Home Electric");
                Assert.That(loaded, Is.TypeOf<UntrackedAccount>());
                Assert.That(loaded!.Type, Is.EqualTo(AccountType.Utility));
                Assert.That(loaded.Institution, Is.EqualTo("Duke Energy"));
                Assert.That(loaded.AccountNumber, Is.EqualTo("ABC-123"));
                Assert.That(store.GetAllAccounts().Count(), Is.EqualTo(1));
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void TryDelete(string path)
        {
            foreach (var file in new[] { path, path + "-wal", path + "-shm" })
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
