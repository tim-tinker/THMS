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
            SqliteSchema.EnsureColumn(conn, "VehicleEv", "KwhPerSocPoint", "REAL");
            SqliteSchema.EnsureColumn(conn, "VehicleEv", "SocEnergyA2", "REAL NOT NULL DEFAULT 0");
            SqliteSchema.EnsureColumn(conn, "VehicleEv", "SocEnergyA3", "REAL NOT NULL DEFAULT 0");
        }

        public void Upsert(SqliteConnection conn, VehicleEv ev)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO VehicleEv (
                    VehicleId, BatteryCapacityKwh, ChargePortType, KwhPerSocPoint, SocEnergyA2, SocEnergyA3)
                VALUES (
                    @VehicleId, @BatteryCapacityKwh, @ChargePortType, @KwhPerSocPoint, @SocEnergyA2, @SocEnergyA3)
                ON CONFLICT(VehicleId) DO UPDATE SET
                    BatteryCapacityKwh = excluded.BatteryCapacityKwh,
                    ChargePortType = excluded.ChargePortType,
                    KwhPerSocPoint = excluded.KwhPerSocPoint,
                    SocEnergyA2 = excluded.SocEnergyA2,
                    SocEnergyA3 = excluded.SocEnergyA3;";

            cmd.Parameters.AddWithValue("@VehicleId", ev.Id.ToString());
            cmd.Parameters.AddWithValue("@BatteryCapacityKwh", ev.BatteryCapacityKwh);
            cmd.Parameters.AddWithValue("@ChargePortType", ev.ChargePortType);
            cmd.Parameters.AddWithValue("@KwhPerSocPoint", ev.ChargeEnergy is SocEnergyCoefficients energy ? energy.A1 : DBNull.Value);
            cmd.Parameters.AddWithValue("@SocEnergyA2", ev.ChargeEnergy?.A2 ?? 0m);
            cmd.Parameters.AddWithValue("@SocEnergyA3", ev.ChargeEnergy?.A3 ?? 0m);

            cmd.ExecuteNonQuery();
        }

        public (decimal BatteryCapacityKwh, string ChargePortType, SocEnergyCoefficients? ChargeEnergy)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT BatteryCapacityKwh, ChargePortType, KwhPerSocPoint, SocEnergyA2, SocEnergyA3
                FROM VehicleEv
                WHERE VehicleId = @VehicleId;";
            cmd.Parameters.AddWithValue("@VehicleId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            SocEnergyCoefficients? energy = reader.IsDBNull(2)
                ? null
                : new SocEnergyCoefficients(
                    (decimal)(double)reader.GetDouble(2),
                    reader.IsDBNull(3) ? 0m : (decimal)(double)reader.GetDouble(3),
                    reader.IsDBNull(4) ? 0m : (decimal)(double)reader.GetDouble(4));

            return (
                (decimal)(double)reader.GetDouble(0),
                reader.GetString(1),
                energy
            );
        }
    }
}
