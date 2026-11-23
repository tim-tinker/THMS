using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class EvBatteryChargeTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using (var create = conn.CreateCommand())
            {
                create.CommandText =
                @"
                CREATE TABLE IF NOT EXISTS EvBatteryCharges (
                    SessionId TEXT PRIMARY KEY,
                    KwhAdded REAL NOT NULL,
                    IsCalculated INTEGER NOT NULL
                );
                ";
                create.ExecuteNonQuery();
            }

            using var copy = conn.CreateCommand();
            copy.CommandText =
            @"
            INSERT INTO EvBatteryCharges (SessionId, KwhAdded, IsCalculated)
            SELECT b.Id, b.KwhAdded, b.KwhAddedIsCalculated
            FROM BaseEvChargeSessions b
            WHERE NOT EXISTS (
                SELECT 1 FROM EvBatteryCharges c WHERE c.SessionId = b.Id
            );
            ";
            copy.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Guid sessionId, EvBatteryCharge charge)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO EvBatteryCharges (SessionId, KwhAdded, IsCalculated)
            VALUES (@SessionId, @KwhAdded, @IsCalculated)
            ON CONFLICT(SessionId) DO UPDATE SET
                KwhAdded = excluded.KwhAdded,
                IsCalculated = excluded.IsCalculated;
            ";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.Parameters.AddWithValue("@KwhAdded", charge.KwhAdded);
            cmd.Parameters.AddWithValue("@IsCalculated", charge.IsCalculated ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM EvBatteryCharges WHERE SessionId = @SessionId;";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}