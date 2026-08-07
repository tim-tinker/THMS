using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class VehicleEvTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS VehicleEv (
                    VehicleId TEXT PRIMARY KEY,
                    BatteryCapacityKwh REAL NOT NULL,
                    ChargePortType TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Insert(SqliteConnection conn, VehicleEv ev)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO VehicleEv (VehicleId, BatteryCapacityKwh, ChargePortType)
                VALUES (@VehicleId, @BatteryCapacityKwh, @ChargePortType);";

            cmd.Parameters.AddWithValue("@VehicleId", ev.Id.ToString());
            cmd.Parameters.AddWithValue("@BatteryCapacityKwh", ev.BatteryCapacityKwh);
            cmd.Parameters.AddWithValue("@ChargePortType", ev.ChargePortType);

            cmd.ExecuteNonQuery();
        }

        public (decimal BatteryCapacityKwh, string ChargePortType)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT BatteryCapacityKwh, ChargePortType
                FROM VehicleEv
                WHERE VehicleId = @VehicleId;";
            cmd.Parameters.AddWithValue("@VehicleId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                reader.GetString(1)
            );
        }
    }
}
