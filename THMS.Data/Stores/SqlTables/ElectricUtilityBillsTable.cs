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
                    EndDate TEXT PRIMARY KEY,
                    Id TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    GridImportCost REAL NOT NULL,
                    GridExportCredit REAL NOT NULL,
                    DeliveryCharges REAL NOT NULL,
                    FixedCharges REAL NOT NULL,
                    TaxesAndFees REAL NOT NULL,
                    TotalKwh REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, ElectricUtilityBill bill)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO ElectricUtilityBills
                (EndDate, Id, StartDate, GridImportCost, GridExportCredit,
                 DeliveryCharges, FixedCharges, TaxesAndFees, TotalKwh)
                VALUES
                (@EndDate, @Id, @StartDate, @GridImportCost, @GridExportCredit,
                 @DeliveryCharges, @FixedCharges, @TaxesAndFees, @TotalKwh)
                ON CONFLICT(EndDate) DO UPDATE SET
                    StartDate = excluded.StartDate,
                    GridImportCost = excluded.GridImportCost,
                    GridExportCredit = excluded.GridExportCredit,
                    DeliveryCharges = excluded.DeliveryCharges,
                    FixedCharges = excluded.FixedCharges,
                    TaxesAndFees = excluded.TaxesAndFees,
                    TotalKwh = excluded.TotalKwh;", conn);

            cmd.Parameters.AddWithValue("@EndDate", bill.EndDate);
            cmd.Parameters.AddWithValue("@Id", bill.Id.ToString());
            cmd.Parameters.AddWithValue("@StartDate", bill.StartDate);
            cmd.Parameters.AddWithValue("@GridImportCost", bill.GridImportCost);
            cmd.Parameters.AddWithValue("@GridExportCredit", bill.GridExportCredit);
            cmd.Parameters.AddWithValue("@DeliveryCharges", bill.DeliveryCharges);
            cmd.Parameters.AddWithValue("@FixedCharges", bill.FixedCharges);
            cmd.Parameters.AddWithValue("@TaxesAndFees", bill.TaxesAndFees);
            cmd.Parameters.AddWithValue("@TotalKwh", bill.TotalKwh);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<ElectricUtilityBill> GetRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, StartDate, EndDate,
                       GridImportCost, GridExportCredit,
                       DeliveryCharges, FixedCharges, TaxesAndFees,
                       TotalKwh
                FROM ElectricUtilityBills
                WHERE StartDate >= @Start AND EndDate <= @End
                ORDER BY StartDate;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<ElectricUtilityBill>();

            while (reader.Read())
            {
                list.Add(new ElectricUtilityBill
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartDate = reader.GetDateTime(1),
                    EndDate = reader.GetDateTime(2),
                    GridImportCost = reader.GetDecimal(3),
                    GridExportCredit = reader.GetDecimal(4),
                    DeliveryCharges = reader.GetDecimal(5),
                    FixedCharges = reader.GetDecimal(6),
                    TaxesAndFees = reader.GetDecimal(7),
                    TotalKwh = reader.GetDecimal(8)
                });
            }

            return list;
        }
    }
}
