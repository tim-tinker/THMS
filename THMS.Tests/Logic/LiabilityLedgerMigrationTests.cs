using Microsoft.Data.Sqlite;
using THMS.Data.Stores;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class LiabilityLedgerMigrationTests
    {
        [Test]
        public void ToLedgerAmount_MakesInterestNegativeAndPaymentsPositive()
        {
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(224.76m, "Interest", "Interest"), Is.EqualTo(-224.76m));
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(30000m, "Loan Funding", "AMEX Loan Disbursement"), Is.EqualTo(-30000m));
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(953.86m, "Loan Payment", "Payment Received - Thank You"), Is.EqualTo(953.86m));
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(-1673.80m, "Loan Payment", "Mortgage payment"), Is.EqualTo(1673.80m));
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(1706.15m, "Loan Payment", "Mortgage Principal"), Is.EqualTo(1706.15m));
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(74.43m, "Interest", "Payment Kinecta FCU"), Is.EqualTo(-74.43m));
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(12m, "Groceries", "Store"), Is.EqualTo(-12m));
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(5m, "", "Interest"), Is.EqualTo(-5m));
            Assert.That(LiabilityLedgerMigration.ToLedgerAmount(10m, null, null), Is.EqualTo(-10m));
        }

        [Test]
        public void Apply_FlipsPrincipalAndActivityOnce()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-liability-{Guid.NewGuid():N}.db");
            try
            {
                var loanId = Guid.NewGuid().ToString();
                var mortgageId = Guid.NewGuid().ToString();
                var checkingId = Guid.NewGuid().ToString();
                var interestId = Guid.NewGuid().ToString();
                var paymentId = Guid.NewGuid().ToString();
                var checkingTxnId = Guid.NewGuid().ToString();
                var splitParent = paymentId;

                using (var conn = Open(path))
                {
                    Execute(conn, @"
                        CREATE TABLE LoanAccounts (AccountId TEXT PRIMARY KEY, Principal REAL NOT NULL);
                        CREATE TABLE MortgageAccounts (AccountId TEXT PRIMARY KEY, Principal REAL NOT NULL);
                        CREATE TABLE PostedTransactions (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Date TEXT NOT NULL,
                            Description TEXT, Amount REAL NOT NULL, Category TEXT);
                        CREATE TABLE FutureSingleTransactions (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Date TEXT NOT NULL,
                            Description TEXT, Amount REAL NOT NULL, Category TEXT);
                        CREATE TABLE RecurringSingleTransactionRules (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Date TEXT NOT NULL,
                            Description TEXT, Amount REAL NOT NULL, Category TEXT, FinalPaymentAmount REAL);
                        CREATE TABLE SplitTransactionRows (
                            Id TEXT PRIMARY KEY, ParentTransactionId TEXT NOT NULL, Amount REAL NOT NULL);
                        CREATE TABLE PostedTransferTransactions (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Amount REAL NOT NULL);
                        INSERT INTO LoanAccounts (AccountId, Principal) VALUES (@Loan, 27796.27);
                        INSERT INTO MortgageAccounts (AccountId, Principal) VALUES (@Mortgage, 37884.67);
                        INSERT INTO PostedTransactions (Id, AccountId, Date, Description, Amount, Category)
                            VALUES (@Interest, @Loan, '2026-10-08', 'Interest', 213.79, 'Interest');
                        INSERT INTO PostedTransactions (Id, AccountId, Date, Description, Amount, Category)
                            VALUES (@Payment, @Mortgage, '2026-01-02', 'Mortgage payment', -1673.80, 'Loan Payment');
                        INSERT INTO PostedTransactions (Id, AccountId, Date, Description, Amount, Category)
                            VALUES (@CheckingTxn, @Checking, '2026-01-02', 'Mortgage payment', -1805.10, 'Loan Payment');
                        INSERT INTO SplitTransactionRows (Id, ParentTransactionId, Amount)
                            VALUES (@Split, @Payment, -1673.80);
                        INSERT INTO FutureSingleTransactions (Id, AccountId, Date, Description, Amount, Category)
                            VALUES (@Future, @Loan, '2026-11-08', 'Interest', 200, 'Interest');
                        INSERT INTO RecurringSingleTransactionRules
                            (Id, AccountId, Date, Description, Amount, Category, FinalPaymentAmount)
                            VALUES (@Rule, @Mortgage, '2026-11-01', 'Payment', -1805.10, 'Loan Payment', -900);
                        INSERT INTO PostedTransferTransactions (Id, AccountId, Amount)
                            VALUES (@Transfer, @Loan, 500);",
                        ("@Loan", loanId),
                        ("@Mortgage", mortgageId),
                        ("@Checking", checkingId),
                        ("@Interest", interestId),
                        ("@Payment", paymentId),
                        ("@CheckingTxn", checkingTxnId),
                        ("@Split", Guid.NewGuid().ToString()),
                        ("@Future", Guid.NewGuid().ToString()),
                        ("@Rule", Guid.NewGuid().ToString()),
                        ("@Transfer", Guid.NewGuid().ToString()));

                    LiabilityLedgerMigration.Apply(conn);
                    LiabilityLedgerMigration.Apply(conn);

                    Assert.That(Scalar(conn, "SELECT Principal FROM LoanAccounts"), Is.EqualTo(-27796.27));
                    Assert.That(Scalar(conn, "SELECT Principal FROM MortgageAccounts"), Is.EqualTo(-37884.67));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransactions WHERE Id = @Id", ("@Id", interestId)), Is.EqualTo(-213.79));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransactions WHERE Id = @Id", ("@Id", paymentId)), Is.EqualTo(1673.80));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransactions WHERE Id = @Id", ("@Id", checkingTxnId)), Is.EqualTo(-1805.10));
                    Assert.That(Scalar(conn, "SELECT Amount FROM SplitTransactionRows WHERE ParentTransactionId = @Id", ("@Id", splitParent)), Is.EqualTo(1673.80));
                    Assert.That(Scalar(conn, "SELECT Amount FROM FutureSingleTransactions"), Is.EqualTo(-200));
                    Assert.That(Scalar(conn, "SELECT Amount FROM RecurringSingleTransactionRules"), Is.EqualTo(1805.10));
                    Assert.That(Scalar(conn, "SELECT FinalPaymentAmount FROM RecurringSingleTransactionRules"), Is.EqualTo(900));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransferTransactions"), Is.EqualTo(500));
                }
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void Apply_FlipsPrincipalBeforeActivityTablesExist()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-liability-{Guid.NewGuid():N}.db");
            try
            {
                var loanId = Guid.NewGuid().ToString();
                using (var conn = Open(path))
                {
                    Execute(conn, @"
                        CREATE TABLE LoanAccounts (AccountId TEXT PRIMARY KEY, Principal REAL NOT NULL);
                        CREATE TABLE MortgageAccounts (AccountId TEXT PRIMARY KEY, Principal REAL NOT NULL);
                        INSERT INTO LoanAccounts (AccountId, Principal) VALUES (@Loan, 1000);
                        INSERT INTO MortgageAccounts (AccountId, Principal) VALUES (@Mortgage, 2000);",
                        ("@Loan", loanId),
                        ("@Mortgage", Guid.NewGuid().ToString()));
                    LiabilityLedgerMigration.Apply(conn);
                    Assert.That(Scalar(conn, "SELECT Principal FROM LoanAccounts"), Is.EqualTo(-1000));

                    Execute(conn, @"
                        CREATE TABLE PostedTransactions (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Date TEXT NOT NULL,
                            Description TEXT, Amount REAL NOT NULL, Category TEXT);
                        CREATE TABLE FutureSingleTransactions (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Date TEXT NOT NULL,
                            Description TEXT, Amount REAL NOT NULL, Category TEXT);
                        CREATE TABLE RecurringSingleTransactionRules (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Date TEXT NOT NULL,
                            Description TEXT, Amount REAL NOT NULL, Category TEXT, FinalPaymentAmount REAL);
                        CREATE TABLE SplitTransactionRows (
                            Id TEXT PRIMARY KEY, ParentTransactionId TEXT NOT NULL, Amount REAL NOT NULL);
                        INSERT INTO PostedTransactions (Id, AccountId, Date, Description, Amount, Category)
                            VALUES (@Id, @Loan, '2026-10-08', 'Interest', 40, 'Interest');",
                        ("@Id", Guid.NewGuid().ToString()),
                        ("@Loan", loanId));
                    LiabilityLedgerMigration.Apply(conn);

                    Assert.That(Scalar(conn, "SELECT Principal FROM LoanAccounts"), Is.EqualTo(-1000));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransactions"), Is.EqualTo(-40));
                }
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void Apply_NegatesPlaidCreditCardAmountsOnce()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-liability-{Guid.NewGuid():N}.db");
            try
            {
                var cardId = Guid.NewGuid().ToString();
                var checkingId = Guid.NewGuid().ToString();
                var chargeId = Guid.NewGuid().ToString();
                var paymentId = Guid.NewGuid().ToString();
                var fileId = Guid.NewGuid().ToString();
                var checkingTxnId = Guid.NewGuid().ToString();
                var splitId = Guid.NewGuid().ToString();
                var transferId = Guid.NewGuid().ToString();

                using (var conn = Open(path))
                {
                    Execute(conn, @"
                        CREATE TABLE CreditAccounts (AccountId TEXT PRIMARY KEY);
                        CREATE TABLE PostedTransactions (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Date TEXT NOT NULL,
                            Description TEXT, Amount REAL NOT NULL, Category TEXT,
                            ExternalTransactionId TEXT);
                        CREATE TABLE PostedTransferTransactions (
                            Id TEXT PRIMARY KEY, AccountId TEXT NOT NULL, Amount REAL NOT NULL,
                            ExternalTransactionId TEXT);
                        CREATE TABLE SplitTransactionRows (
                            Id TEXT PRIMARY KEY, ParentTransactionId TEXT NOT NULL, Amount REAL NOT NULL);
                        INSERT INTO CreditAccounts (AccountId) VALUES (@Card);
                        INSERT INTO PostedTransactions (Id, AccountId, Date, Description, Amount, Category, ExternalTransactionId)
                            VALUES (@Charge, @Card, '2026-10-09', 'AMAZON', 19.28, 'Restaurants', 'plaid-charge');
                        INSERT INTO PostedTransactions (Id, AccountId, Date, Description, Amount, Category, ExternalTransactionId)
                            VALUES (@Payment, @Card, '2026-09-25', 'ONLINE PAYMENT - THANK YOU', -1600, 'Credit Card Payment', 'plaid-payment');
                        INSERT INTO PostedTransactions (Id, AccountId, Date, Description, Amount, Category, ExternalTransactionId)
                            VALUES (@File, @Card, '2026-10-09', 'Interest Charge on Purchases', -278.69, 'Interest', '');
                        INSERT INTO PostedTransactions (Id, AccountId, Date, Description, Amount, Category, ExternalTransactionId)
                            VALUES (@CheckingTxn, @Checking, '2026-10-09', 'Market', -12.34, 'Groceries', 'plaid-checking');
                        INSERT INTO SplitTransactionRows (Id, ParentTransactionId, Amount)
                            VALUES (@Split, @Charge, 19.28);
                        INSERT INTO PostedTransferTransactions (Id, AccountId, Amount, ExternalTransactionId)
                            VALUES (@Transfer, @Card, 6.55, 'plaid-transfer');",
                        ("@Card", cardId),
                        ("@Checking", checkingId),
                        ("@Charge", chargeId),
                        ("@Payment", paymentId),
                        ("@File", fileId),
                        ("@CheckingTxn", checkingTxnId),
                        ("@Split", splitId),
                        ("@Transfer", transferId));

                    LiabilityLedgerMigration.Apply(conn);
                    LiabilityLedgerMigration.Apply(conn);

                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransactions WHERE Id = @Id", ("@Id", chargeId)), Is.EqualTo(-19.28));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransactions WHERE Id = @Id", ("@Id", paymentId)), Is.EqualTo(1600));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransactions WHERE Id = @Id", ("@Id", fileId)), Is.EqualTo(-278.69));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransactions WHERE Id = @Id", ("@Id", checkingTxnId)), Is.EqualTo(-12.34));
                    Assert.That(Scalar(conn, "SELECT Amount FROM SplitTransactionRows WHERE Id = @Id", ("@Id", splitId)), Is.EqualTo(-19.28));
                    Assert.That(Scalar(conn, "SELECT Amount FROM PostedTransferTransactions WHERE Id = @Id", ("@Id", transferId)), Is.EqualTo(-6.55));
                }
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static SqliteConnection Open(string path)
        {
            var conn = new SqliteConnection($"Data Source={path}");
            conn.Open();
            return conn;
        }

        private static void Execute(SqliteConnection conn, string sql, params (string Name, string Value)[] parameters)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value);
            cmd.ExecuteNonQuery();
        }

        private static double Scalar(SqliteConnection conn, string sql, params (string Name, string Value)[] parameters)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value);
            return Convert.ToDouble(cmd.ExecuteScalar());
        }
    }
}
