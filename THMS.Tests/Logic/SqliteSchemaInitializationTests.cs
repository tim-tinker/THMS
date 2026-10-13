using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class SqliteSchemaInitializationTests
    {
        [Test]
        public void ProductionStores_InitializeSchemaOnNewAndExistingDatabase()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-schema-{Guid.NewGuid():N}.db");
            try
            {
                using (var conn = new SqliteConnection($"Data Source={path}"))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        CREATE TABLE PostedTransactions (
                            Id TEXT PRIMARY KEY,
                            AccountId TEXT NOT NULL,
                            Date TEXT NOT NULL,
                            Description TEXT,
                            Amount REAL NOT NULL,
                            Category TEXT
                        );";
                    cmd.ExecuteNonQuery();
                }

                CreateAll(path);
                CreateAll(path);

                var energy = new SQLiteEnergyDataStore(path);
                var solar = energy.GetSolarProductionIntervals(DateTime.Today, DateTime.Today.AddDays(1)).ToList();
                Assert.That(solar, Is.Empty);

                var checking = Guid.NewGuid();
                var savings = Guid.NewGuid();
                var transactions = new SQLiteTransactionDataStore(path);
                transactions.AddPostedTransaction(new PostedTransaction
                {
                    AccountId = checking,
                    Date = new DateTime(2026, 1, 5),
                    Description = "Paycheck",
                    Amount = 1234,
                    Splits =
                    [
                        new() { Amount = -500, Type = SplitType.Transfer, TransferAccountId = savings }
                    ]
                });
                var orchestrator = new TransactionOrchestrator(transactions);
                Assert.That(orchestrator.SumPostedAmountsBefore(savings, new DateTime(2026, 1, 6)), Is.EqualTo(500m));
                Assert.That(
                    orchestrator.GetTransactionsForAccount(savings, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31))
                        .IncomingTransferSplitPosted.Count(),
                    Is.EqualTo(1));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                {
                    var file = path + suffix;
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        private static void CreateAll(string path)
        {
            _ = new SQLiteTransactionDataStore(path);
            _ = new SQLiteAccountDataStore(path);
            _ = new SQLiteAccountStatementDataStore(path);
            _ = new SQLiteFinanceDataStore(path);
            _ = new SQLiteEnergyDataStore(path);
            _ = new SQLiteVehicleDataStore(path);
        }
    }
}
