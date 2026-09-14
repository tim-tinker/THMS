using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

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
                StatementBalance = -1
            }), Has.Some.Contains("Statement balance"));

            Assert.That(AccountStatementValidator.Validate(new BankStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                StatementBalance = 0
            }), Is.Empty);

            Assert.That(AccountStatementValidator.Validate(new LoanStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(-1),
                AmountDue = 10,
                StatementBalance = -1
            }), Has.Some.Contains("Due date").And.Some.Contains("Statement balance"));

            Assert.That(AccountStatementValidator.Validate(new MortgageStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 100,
                EscrowBalance = -5
            }), Has.Some.Contains("escrow"));

            Assert.That(AccountStatementValidator.Validate(new CreditCardStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 50,
                StatementBalance = -1,
                Promotions = [new() { Amount = 10, Deadline = default }]
            }), Has.Some.Contains("Statement balance").And.Some.Contains("deadline"));

            Assert.That(AccountStatementValidator.Validate(new UtilityStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 50,
                Usage = [new() { Type = "kWh", Amount = 0 }],
                Charges = [new() { Description = "Energy", Amount = 40 }]
            }), Has.Some.Contains("positive amount").And.Some.Contains("must equal amount due"));

            Assert.That(AccountStatementValidator.Validate(new ServiceStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 15,
                Charges = [new() { Description = "Base", Amount = 10 }]
            }), Has.Some.Contains("must equal amount due"));

            Assert.That(AccountStatementValidator.Validate(new InsuranceStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                AmountDue = 100
            }), Is.Empty);

            Assert.That(AccountStatementValidator.Validate(ValidUtility()), Is.Empty);
            Assert.That(AccountStatementValidator.Validate(new BankStatement
            {
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                StatementBalance = 131.25m
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
                StatementBalance = 350
            });
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                StatementBalance = 600
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
                    StatementBalance = 1090.73m
                },
                new LoanStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-20),
                    DueDate = DateTime.Today.AddDays(5),
                    AmountDue = 200,
                    StatementBalance = 5000
                },
                new MortgageStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-20),
                    DueDate = DateTime.Today.AddDays(6),
                    AmountDue = 1800,
                    StatementBalance = 200000,
                    EscrowBalance = 1200
                },
                new CreditCardStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-15),
                    DueDate = DateTime.Today.AddDays(10),
                    AmountDue = 300,
                    StatementBalance = 300,
                    Promotions = [new() { AccountId = accountId, Amount = 50, Deadline = DateTime.Today.AddDays(8), Type = PromoType.LumpSum }]
                },
                new UtilityStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-10),
                    DueDate = DateTime.Today.AddDays(12),
                    AmountDue = 80,
                    Usage = [new() { Type = "kWh", Amount = 400, Rate = 0.20m }],
                    Charges = [new() { Description = "Energy", Amount = 80 }]
                },
                new ServiceStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-5),
                    DueDate = DateTime.Today.AddDays(15),
                    AmountDue = 15,
                    Charges = [new() { Description = "Streaming", Amount = 15 }]
                },
                new InsuranceStatement
                {
                    AccountId = accountId,
                    StatementDate = DateTime.Today.AddDays(-8),
                    DueDate = DateTime.Today.AddDays(20),
                    AmountDue = 110
                }
            ];

            foreach (var statement in statements)
                store.Save(statement);

            Assert.That(store.GetForAccount(accountId), Has.Count.EqualTo(7));
            Assert.That(store.GetUpcoming(DateTime.Today), Has.Count.EqualTo(7));
            var bank = (BankStatement)store.Get(statements[0].Id)!;
            Assert.That(bank.StatementBalance, Is.EqualTo(1090.73m));
            Assert.That(((LoanStatement)store.Get(statements[1].Id)!).StatementBalance, Is.EqualTo(5000m));
            var mortgage = (MortgageStatement)store.Get(statements[2].Id)!;
            Assert.That(mortgage.StatementBalance, Is.EqualTo(200000m));
            Assert.That(mortgage.EscrowBalance, Is.EqualTo(1200m));
            var card = (CreditCardStatement)store.Get(statements[3].Id)!;
            Assert.That(card.Promotions, Has.Count.EqualTo(1));
            Assert.That(card.Promotions[0].Amount, Is.EqualTo(50m));
            var utility = (UtilityStatement)store.Get(statements[4].Id)!;
            Assert.That(utility.Usage[0].Amount, Is.EqualTo(400m));
            Assert.That(utility.Charges[0].Amount, Is.EqualTo(80m));
            Assert.That(((ServiceStatement)store.Get(statements[5].Id)!).Charges[0].Description, Is.EqualTo("Streaming"));
            Assert.That(((InsuranceStatement)store.Get(statements[6].Id)!).AmountDue, Is.EqualTo(110m));

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

        [Test]
        public void AccountStatementListRow_MapsCommonAndTypeSpecificFields()
        {
            var card = new CreditCardStatement
            {
                Id = Guid.NewGuid(),
                StatementDate = new DateTime(2026, 8, 15),
                DueDate = new DateTime(2026, 9, 10),
                AmountDue = 220.50m,
                StatementBalance = 400,
                Notes = "August",
                Promotions =
                [
                    new() { Amount = 50, Deadline = new DateTime(2026, 9, 1), Type = PromoType.LumpSum }
                ]
            };

            var row = AccountStatementListRow.From(card, -12.25m);
            Assert.That(row.Id, Is.EqualTo(card.Id));
            Assert.That(row.Type, Is.EqualTo("Credit Card"));
            Assert.That(row.StatementDate, Is.EqualTo(card.StatementDate.ToString("d")));
            Assert.That(row.DueDate, Is.EqualTo(card.DueDate.ToString("d")));
            Assert.That(row.AmountDue, Is.EqualTo(220.50m.ToString("c2")));
            Assert.That(row.StatementBalance, Is.EqualTo(400m.ToString("c2")));
            Assert.That(row.Interest, Is.EqualTo((-12.25m).ToString("c2")));
            Assert.That(row.Promotions, Does.Contain("Lump sum").And.Contain(50m.ToString("c2")));
            Assert.That(row.Usage, Is.EqualTo(AccountStatementListRow.NotApplicable));
            Assert.That(row.Notes, Is.EqualTo("August"));
            Assert.That(AccountStatementListRow.DisplayType(StatementType.Bank), Is.EqualTo("Bank"));
        }

        [Test]
        public void AccountStatementListRow_BankLeavesObligationFieldsNotApplicable()
        {
            var bank = new BankStatement
            {
                StatementDate = new DateTime(2026, 8, 31),
                DueDate = new DateTime(2026, 8, 31),
                StatementBalance = 1148.25m,
                Notes = "August checking"
            };

            var row = AccountStatementListRow.From(bank, 1.25m);
            Assert.That(row.Type, Is.EqualTo("Bank"));
            Assert.That(row.DueDate, Is.EqualTo(AccountStatementListRow.NotApplicable));
            Assert.That(row.AmountDue, Is.EqualTo(AccountStatementListRow.NotApplicable));
            Assert.That(row.Interest, Is.EqualTo(1.25m.ToString("c2")));
            Assert.That(row.StatementBalance, Is.EqualTo(1148.25m.ToString("c2")));
            Assert.That(row.Promotions, Is.EqualTo(AccountStatementListRow.NotApplicable));
            Assert.That(row.Notes, Is.EqualTo("August checking"));
        }

        [Test]
        public void AccountStatementListRow_EmptyApplicableCollectionsAreBlank()
        {
            var card = AccountStatementListRow.From(new CreditCardStatement
            {
                StatementDate = new DateTime(2026, 8, 15),
                DueDate = new DateTime(2026, 9, 10)
            });
            Assert.That(card.Promotions, Is.EqualTo(""));
            Assert.That(card.Usage, Is.EqualTo(AccountStatementListRow.NotApplicable));

            var utility = AccountStatementListRow.From(new UtilityStatement
            {
                StatementDate = new DateTime(2026, 8, 15),
                DueDate = new DateTime(2026, 9, 10),
                AmountDue = 80
            });
            Assert.That(utility.Usage, Is.EqualTo(""));
            Assert.That(utility.Charges, Is.EqualTo(""));
            Assert.That(utility.Promotions, Is.EqualTo(AccountStatementListRow.NotApplicable));
            Assert.That(utility.Interest, Is.EqualTo(AccountStatementListRow.NotApplicable));
        }

        [Test]
        public void StatementPeriodInterest_SumsInterestCategoryInStatementWindow()
        {
            var accountId = Guid.NewGuid();
            var older = new BankStatement
            {
                AccountId = accountId,
                StatementDate = new DateTime(2026, 7, 31)
            };
            var current = new BankStatement
            {
                AccountId = accountId,
                StatementDate = new DateTime(2026, 8, 31)
            };
            PostedTransaction[] posted =
            [
                new()
                {
                    AccountId = accountId,
                    Date = new DateTime(2026, 7, 31),
                    Amount = 0.40m,
                    CategoryId = DefaultExpenseCategories.InterestId,
                    Category = DefaultExpenseCategories.Interest
                },
                new()
                {
                    AccountId = accountId,
                    Date = new DateTime(2026, 8, 15),
                    Amount = 1.25m,
                    CategoryId = DefaultExpenseCategories.InterestId,
                    Category = DefaultExpenseCategories.Interest
                },
                new()
                {
                    AccountId = accountId,
                    Date = new DateTime(2026, 8, 20),
                    Amount = 50,
                    CategoryId = DefaultExpenseCategories.PaymentId,
                    Category = DefaultExpenseCategories.Payment
                },
                new()
                {
                    AccountId = accountId,
                    Date = new DateTime(2026, 8, 22),
                    Amount = -200,
                    Splits =
                    [
                        new() { Amount = -160, Type = SplitType.Principal, CategoryId = DefaultExpenseCategories.PaymentId, Category = DefaultExpenseCategories.Payment },
                        new() { Amount = -40, Type = SplitType.Interest, CategoryId = DefaultExpenseCategories.InterestId, Category = DefaultExpenseCategories.Interest }
                    ]
                },
                new()
                {
                    AccountId = accountId,
                    Date = new DateTime(2026, 9, 1),
                    Amount = 0.10m,
                    CategoryId = DefaultExpenseCategories.InterestId,
                    Category = DefaultExpenseCategories.Interest
                }
            ];

            Assert.That(StatementPeriodInterest.Compute(current, [older, current], posted), Is.EqualTo(-38.75m));
            Assert.That(StatementPeriodInterest.Compute(older, [older, current], posted), Is.EqualTo(0.40m));
            Assert.That(
                AccountStatementListRow.From(new UtilityStatement { StatementDate = DateTime.Today }).Interest,
                Is.EqualTo(AccountStatementListRow.NotApplicable));
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
