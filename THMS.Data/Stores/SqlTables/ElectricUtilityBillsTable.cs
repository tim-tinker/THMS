using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Billing;

namespace THMS.Data.Stores.SqlTables
{
    public class ElectricUtilityBillsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ElectricUtilityBills (
                    Id TEXT PRIMARY KEY,
                    BillingDate TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    KwhUsage INTEGER NOT NULL,
                    DaysInCycle INTEGER NOT NULL,
                    BaseCharge REAL NOT NULL,
                    EnergyChargeRate REAL NOT NULL,
                    EnergyCharge REAL NOT NULL,
                    ExportKwh REAL NOT NULL,
                    ExportCreditRate REAL NOT NULL,
                    ExportCredit REAL NOT NULL,
                    DeliveryCharge REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();

            EnsureColumn(conn, "BillingDate", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(conn, "KwhUsage", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "DaysInCycle", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "BaseCharge", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "EnergyChargeRate", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "EnergyCharge", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "ExportKwh", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "ExportCreditRate", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "ExportCredit", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "DeliveryCharge", "REAL NOT NULL DEFAULT 0");

            // Legacy DBs used EndDate as PK; ensure Id is unique for ON CONFLICT(Id).
            using var index = conn.CreateCommand();
            index.CommandText =
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_ElectricUtilityBills_Id ON ElectricUtilityBills(Id);";
            index.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE ElectricUtilityBills ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, ElectricUtilityBill bill)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO ElectricUtilityBills
                (Id, BillingDate, StartDate, EndDate, KwhUsage, DaysInCycle,
                 BaseCharge, EnergyChargeRate, EnergyCharge,
                 ExportKwh, ExportCreditRate, ExportCredit, DeliveryCharge)
                VALUES
                (@Id, @BillingDate, @StartDate, @EndDate, @KwhUsage, @DaysInCycle,
                 @BaseCharge, @EnergyChargeRate, @EnergyCharge,
                 @ExportKwh, @ExportCreditRate, @ExportCredit, @DeliveryCharge)
                ON CONFLICT(Id) DO UPDATE SET
                    BillingDate = excluded.BillingDate,
                    StartDate = excluded.StartDate,
                    EndDate = excluded.EndDate,
                    KwhUsage = excluded.KwhUsage,
                    DaysInCycle = excluded.DaysInCycle,
                    BaseCharge = excluded.BaseCharge,
                    EnergyChargeRate = excluded.EnergyChargeRate,
                    EnergyCharge = excluded.EnergyCharge,
                    ExportKwh = excluded.ExportKwh,
                    ExportCreditRate = excluded.ExportCreditRate,
                    ExportCredit = excluded.ExportCredit,
                    DeliveryCharge = excluded.DeliveryCharge;", conn);

            Bind(cmd, bill);
            cmd.ExecuteNonQuery();
        }

        public ElectricUtilityBill? GetById(SqliteConnection conn, Guid billId)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, BillingDate, StartDate, EndDate,
                       KwhUsage, DaysInCycle,
                       BaseCharge, EnergyChargeRate, EnergyCharge,
                       ExportKwh, ExportCreditRate, ExportCredit, DeliveryCharge
                FROM ElectricUtilityBills
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", billId.ToString());

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public ElectricUtilityBill? GetForDate(SqliteConnection conn, DateTime date)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, BillingDate, StartDate, EndDate,
                       KwhUsage, DaysInCycle,
                       BaseCharge, EnergyChargeRate, EnergyCharge,
                       ExportKwh, ExportCreditRate, ExportCredit, DeliveryCharge
                FROM ElectricUtilityBills
                WHERE StartDate <= @Date AND EndDate >= @Date
                ORDER BY EndDate DESC
                LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("@Date", date);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<ElectricUtilityBill> GetRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, BillingDate, StartDate, EndDate,
                       KwhUsage, DaysInCycle,
                       BaseCharge, EnergyChargeRate, EnergyCharge,
                       ExportKwh, ExportCreditRate, ExportCredit, DeliveryCharge
                FROM ElectricUtilityBills
                WHERE (StartDate >= @Start AND StartDate <= @End)
                   OR (EndDate >= @Start AND EndDate <= @End)
                ORDER BY StartDate;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<ElectricUtilityBill>();

            while (reader.Read())
                list.Add(Read(reader));

            return list;
        }

        public ElectricUtilityBill? GetLatest(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, BillingDate, StartDate, EndDate,
                       KwhUsage, DaysInCycle,
                       BaseCharge, EnergyChargeRate, EnergyCharge,
                       ExportKwh, ExportCreditRate, ExportCredit, DeliveryCharge
                FROM ElectricUtilityBills
                ORDER BY EndDate DESC
                LIMIT 1;", conn);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        private static void Bind(SqliteCommand cmd, ElectricUtilityBill bill)
        {
            cmd.Parameters.AddWithValue("@Id", bill.Id.ToString());
            cmd.Parameters.AddWithValue("@BillingDate", bill.BillingDate);
            cmd.Parameters.AddWithValue("@StartDate", bill.StartDate);
            cmd.Parameters.AddWithValue("@EndDate", bill.EndDate);
            cmd.Parameters.AddWithValue("@KwhUsage", bill.KwhUsage);
            cmd.Parameters.AddWithValue("@DaysInCycle", bill.DaysInCycle);
            cmd.Parameters.AddWithValue("@BaseCharge", bill.BaseCharge);
            cmd.Parameters.AddWithValue("@EnergyChargeRate", bill.EnergyChargeRate);
            cmd.Parameters.AddWithValue("@EnergyCharge", bill.EnergyCharge);
            cmd.Parameters.AddWithValue("@ExportKwh", bill.ExportKwh);
            cmd.Parameters.AddWithValue("@ExportCreditRate", bill.ExportCreditRate);
            cmd.Parameters.AddWithValue("@ExportCredit", bill.ExportCredit);
            cmd.Parameters.AddWithValue("@DeliveryCharge", bill.DeliveryCharge);
        }

        private static ElectricUtilityBill Read(SqliteDataReader reader)
        {
            return new ElectricUtilityBill
            {
                Id = Guid.Parse(reader.GetString(0)),
                BillingDate = reader.IsDBNull(1) || reader.GetString(1) == string.Empty
                    ? default
                    : reader.GetDateTime(1),
                StartDate = reader.GetDateTime(2),
                EndDate = reader.GetDateTime(3),
                KwhUsage = reader.GetInt32(4),
                DaysInCycle = reader.GetInt32(5),
                BaseCharge = (decimal)(double)reader.GetDouble(6),
                EnergyChargeRate = (decimal)(double)reader.GetDouble(7),
                EnergyCharge = (decimal)(double)reader.GetDouble(8),
                ExportKwh = (decimal)(double)reader.GetDouble(9),
                ExportCreditRate = (decimal)(double)reader.GetDouble(10),
                ExportCredit = (decimal)(double)reader.GetDouble(11),
                DeliveryCharge = (decimal)(double)reader.GetDouble(12)
            };
        }
    }
}
