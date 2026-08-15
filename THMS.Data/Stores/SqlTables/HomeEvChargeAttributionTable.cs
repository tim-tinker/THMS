using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class HomeEvChargeAttributionTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS HomeEvChargeAttributions (
                SessionId TEXT PRIMARY KEY,
                GridKwh REAL NOT NULL,
                SolarKwh REAL NOT NULL,
                BatteryKwh REAL NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Guid sessionId, HomeEvChargeAttribution attribution)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO HomeEvChargeAttributions (SessionId, GridKwh, SolarKwh, BatteryKwh)
            VALUES (@SessionId, @GridKwh, @SolarKwh, @BatteryKwh)
            ON CONFLICT(SessionId) DO UPDATE SET
                GridKwh = excluded.GridKwh,
                SolarKwh = excluded.SolarKwh,
                BatteryKwh = excluded.BatteryKwh;
            ";

            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.Parameters.AddWithValue("@GridKwh", attribution.GridKwh);
            cmd.Parameters.AddWithValue("@SolarKwh", attribution.SolarKwh);
            cmd.Parameters.AddWithValue("@BatteryKwh", attribution.BatteryKwh);
            cmd.ExecuteNonQuery();
        }

        public HomeEvChargeAttribution? GetBySessionId(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT GridKwh, SolarKwh, BatteryKwh
            FROM HomeEvChargeAttributions
            WHERE SessionId = @SessionId;
            ";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new HomeEvChargeAttribution
            {
                GridKwh = (decimal)(double)reader.GetDouble(0),
                SolarKwh = (decimal)(double)reader.GetDouble(1),
                BatteryKwh = (decimal)(double)reader.GetDouble(2)
            };
        }

        public void Delete(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM HomeEvChargeAttributions WHERE SessionId = @SessionId;";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
