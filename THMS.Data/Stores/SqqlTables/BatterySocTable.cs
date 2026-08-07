using Microsoft.Data.Sqlite;
using THMS.Domain.Energy;

namespace THMS.Data.Stores
{
    public class BatterySocTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS BatterySoc (
                Timestamp TEXT PRIMARY KEY,
                SocPercent REAL NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public IReadOnlyCollection<BatterySocRecord> GetRange(
            SqliteConnection conn, DateTime start, DateTime end)
        {
            var list = new List<BatterySocRecord>();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
                SELECT Timestamp, SocPercent
                FROM BatterySoc
                WHERE Timestamp >= @Start AND Timestamp <= @End
                ORDER BY Timestamp;
            ";

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BatterySocRecord
                {
                    Timestamp = reader.GetDateTime(0),
                    SocPercent = reader.GetDecimal(1)
                });
            }

            return list;
        }
    }
}
