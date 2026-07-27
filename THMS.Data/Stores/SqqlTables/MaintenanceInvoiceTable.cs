using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class MaintenanceInvoiceTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS MaintenanceInvoices (
                    Id TEXT PRIMARY KEY,
                    VehicleId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Cost REAL NOT NULL,
                    Description TEXT,
                    Vendor TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        public void Insert(SqliteConnection conn, MaintenanceInvoiceRecord invoice)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO MaintenanceInvoices
                (Id, VehicleId, Date, Cost, Description, Vendor)
                VALUES
                (@Id, @VehicleId, @Date, @Cost, @Description, @Vendor);";

            cmd.Parameters.AddWithValue("@Id", invoice.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", invoice.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@Date", invoice.Date);
            cmd.Parameters.AddWithValue("@Cost", invoice.Cost);
            cmd.Parameters.AddWithValue("@Description", (object?)invoice.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Vendor", (object?)invoice.Vendor ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<MaintenanceInvoiceRecord> GetRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, VehicleId, Date, Cost, Description, Vendor
                FROM MaintenanceInvoices
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new MaintenanceInvoiceRecord
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    VehicleId = Guid.Parse(reader.GetString(1)),
                    Date = reader.GetDateTime(2),
                    Cost = (decimal)(double)reader.GetDouble(3),
                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Vendor = reader.IsDBNull(5) ? null : reader.GetString(5)
                };
            }
        }

        public decimal GetTotalCost(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT SUM(Cost)
                FROM MaintenanceInvoices
                WHERE Date >= @Start AND Date <= @End;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            var result = cmd.ExecuteScalar();
            return result is null or DBNull ? 0m : (decimal)(double)result;
        }
    }
}
