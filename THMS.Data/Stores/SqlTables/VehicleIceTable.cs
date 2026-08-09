using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class VehicleIceTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS VehicleIce (
                    VehicleId TEXT PRIMARY KEY,
                    FuelTankCapacityGallons REAL NOT NULL,
                    FuelType TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, VehicleIce ice)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO VehicleIce (VehicleId, FuelTankCapacityGallons, FuelType)
                VALUES (@VehicleId, @FuelTankCapacityGallons, @FuelType)
                ON CONFLICT(VehicleId) DO UPDATE SET
                    FuelTankCapacityGallons = excluded.FuelTankCapacityGallons,
                    FuelType = excluded.FuelType;";

            cmd.Parameters.AddWithValue("@VehicleId", ice.Id.ToString());
            cmd.Parameters.AddWithValue("@FuelTankCapacityGallons", ice.FuelTankCapacityGallons);
            cmd.Parameters.AddWithValue("@FuelType", ice.FuelType);

            cmd.ExecuteNonQuery();
        }

        public (decimal FuelTankCapacityGallons, string FuelType)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT FuelTankCapacityGallons, FuelType
                FROM VehicleIce
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
