using Microsoft.Data.Sqlite;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;

namespace THMS.Data.Stores
{
    public class SQLiteFinanceDataStore : IFinanceDataStore
    {
        private readonly string _connectionString;

        public SQLiteFinanceDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            InitializeSchema();
        }

        private void InitializeSchema()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS ElectricUtilityBills (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartDate TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                GridImportCost REAL NOT NULL,
                GridExportCredit REAL NOT NULL,
                DeliveryCharges REAL NOT NULL,
                FixedCharges REAL NOT NULL,
                TaxesAndFees REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CommercialChargingCostRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                Cost REAL NOT NULL,
                Vendor TEXT,
                SessionId TEXT
            );

            CREATE TABLE IF NOT EXISTS FinanceTransactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Date TEXT NOT NULL,
                Amount REAL NOT NULL,
                Source TEXT NOT NULL,
                Description TEXT
            );
            ";

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // ELECTRIC UTILITY BILLS
        // ---------------------------------------------------------

        public void AddElectricUtilityBill(ElectricUtilityBill bill)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO ElectricUtilityBills
            (StartDate, EndDate, GridImportCost, GridExportCredit,
             DeliveryCharges, FixedCharges, TaxesAndFees)
            VALUES ($start, $end, $import, $credit, $delivery, $fixed, $taxes);
            ";

            cmd.Parameters.AddWithValue("$start", bill.StartDate);
            cmd.Parameters.AddWithValue("$end", bill.EndDate);
            cmd.Parameters.AddWithValue("$import", bill.GridImportCost);
            cmd.Parameters.AddWithValue("$credit", bill.GridExportCredit);
            cmd.Parameters.AddWithValue("$delivery", bill.DeliveryCharges);
            cmd.Parameters.AddWithValue("$fixed", bill.FixedCharges);
            cmd.Parameters.AddWithValue("$taxes", bill.TaxesAndFees);

            cmd.ExecuteNonQuery();
        }

        public IReadOnlyCollection<ElectricUtilityBill> GetElectricUtilityBills()
        {
            var list = new List<ElectricUtilityBill>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"SELECT StartDate, EndDate, GridImportCost, GridExportCredit,
                         DeliveryCharges, FixedCharges, TaxesAndFees
                  FROM ElectricUtilityBills
                  ORDER BY StartDate";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ElectricUtilityBill
                {
                    StartDate = DateTime.Parse(reader.GetString(0)),
                    EndDate = DateTime.Parse(reader.GetString(1)),
                    GridImportCost = reader.GetDecimal(2),
                    GridExportCredit = reader.GetDecimal(3),
                    DeliveryCharges = reader.GetDecimal(4),
                    FixedCharges = reader.GetDecimal(5),
                    TaxesAndFees = reader.GetDecimal(6)
                });
            }

            return list;
        }

        public IReadOnlyCollection<ElectricUtilityBill> GetAllElectricUtilityBillsRaw()
            => GetElectricUtilityBills();


        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS
        // ---------------------------------------------------------

        public void AddCommercialChargingCostRecord(CommercialChargingCostRecord record)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO CommercialChargingCostRecords
            (Timestamp, Cost, Vendor, SessionId)
            VALUES ($ts, $cost, $vendor, $sessionId);
            ";

            cmd.Parameters.AddWithValue("$ts", record.Timestamp);
            cmd.Parameters.AddWithValue("$cost", record.Cost);
            cmd.Parameters.AddWithValue("$vendor", record.Vendor ?? string.Empty);
            cmd.Parameters.AddWithValue("$sessionId", record.SessionId ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IReadOnlyCollection<CommercialChargingCostRecord> GetCommercialChargingCostRecords()
        {
            var list = new List<CommercialChargingCostRecord>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"SELECT Timestamp, Cost, Vendor, SessionId
                  FROM CommercialChargingCostRecords
                  ORDER BY Timestamp";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CommercialChargingCostRecord
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    Cost = reader.GetDecimal(1),
                    Vendor = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    SessionId = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return list;
        }

        public IReadOnlyCollection<CommercialChargingCostRecord> GetAllCommercialChargingCostRecordsRaw()
            => GetCommercialChargingCostRecords();


        // ---------------------------------------------------------
        // GENERAL FINANCE TRANSACTIONS
        // ---------------------------------------------------------

        public void AddFinanceTransaction(FinanceTransaction transaction)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO FinanceTransactions
            (Date, Amount, Source, Description)
            VALUES ($date, $amount, $source, $desc);
            ";

            cmd.Parameters.AddWithValue("$date", transaction.Date);
            cmd.Parameters.AddWithValue("$amount", transaction.Amount);
            cmd.Parameters.AddWithValue("$source", transaction.Source);
            cmd.Parameters.AddWithValue("$desc", transaction.Description ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IReadOnlyCollection<FinanceTransaction> GetAllTransactions()
        {
            var list = new List<FinanceTransaction>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"SELECT Date, Amount, Source, Description
                  FROM FinanceTransactions
                  ORDER BY Date";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FinanceTransaction
                {
                    Date = DateTime.Parse(reader.GetString(0)),
                    Amount = reader.GetDecimal(1),
                    Source = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return list;
        }
    }
}
