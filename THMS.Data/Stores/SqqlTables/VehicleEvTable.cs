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
                    ChargingPortType TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Insert(SqliteConnection conn, VehicleEv ev)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO VehicleEv (VehicleId, BatteryCapacityKwh, ChargingPortType)
                VALUES (@VehicleId, @BatteryCapacityKwh, @ChargingPortType);";

            cmd.Parameters.AddWithValue("@VehicleId", ev.Id.ToString());
            cmd.Parameters.AddWithValue("@BatteryCapacityKwh", ev.BatteryCapacityKwh);
            cmd.Parameters.AddWithValue("@ChargingPortType", ev.ChargingPortType);

            cmd.ExecuteNonQuery();
        }

        public (decimal BatteryCapacityKwh, string ChargingPortType)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT BatteryCapacityKwh, ChargingPortType
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
