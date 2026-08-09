using Microsoft.Data.Sqlite;
using THMS.Domain.Finance;

namespace THMS.Data.Stores.SqlTables
{
    public class CommercialChargeCostRecordsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CommercialChargeCostRecords (
                    Id TEXT PRIMARY KEY,
                    SessionId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Cost REAL NOT NULL,
                    Vendor TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, CommercialChargeCostRecord record)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO CommercialChargeCostRecords
                (Id, SessionId, Date, Cost, Vendor)
                VALUES
                (@Id, @SessionId, @Date, @Cost, @Vendor)
                ON CONFLICT(Id) DO UPDATE SET
                    SessionId = excluded.SessionId,
                    Date = excluded.Date,
                    Cost = excluded.Cost,
                    Vendor = excluded.Vendor;", conn);

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@SessionId", record.SessionId);
            cmd.Parameters.AddWithValue("@Date", record.Date);
            cmd.Parameters.AddWithValue("@Cost", record.Cost);
            cmd.Parameters.AddWithValue("@Vendor", record.Vendor);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<CommercialChargeCostRecord> GetRange(
            SqliteConnection conn,
            DateTime start,
            DateTime end)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, SessionId, Date, Cost, Vendor
                FROM CommercialChargeCostRecords
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            return ReadAll(cmd);
        }

        public IEnumerable<CommercialChargeCostRecord> GetRangeByVendor(
            SqliteConnection conn,
            string vendor,
            DateTime start,
            DateTime end)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, SessionId, Date, Cost, Vendor
                FROM CommercialChargeCostRecords
                WHERE Vendor = @Vendor
                AND Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@Vendor", vendor);
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            return ReadAll(cmd);
        }

        private static IEnumerable<CommercialChargeCostRecord> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<CommercialChargeCostRecord>();

            while (reader.Read())
            {
                list.Add(new CommercialChargeCostRecord
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    SessionId = reader.GetString(1),
                    Date = reader.GetDateTime(2),
                    Cost = reader.GetDecimal(3),
                    Vendor = reader.GetString(4)
                });
            }

            return list;
        }
    }
}
