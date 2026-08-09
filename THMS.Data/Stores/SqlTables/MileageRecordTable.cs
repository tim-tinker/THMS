using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class MileageRecordTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS MileageRecords (
                Id TEXT PRIMARY KEY,
                VehicleId TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                OdometerMiles REAL NOT NULL,
                Type TEXT NOT NULL
            );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, MileageRecordBase record, string type)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO MileageRecords
            (Id, VehicleId, EndTime, OdometerMiles, Type)
            VALUES
            (@Id, @VehicleId, @EndTime, @OdometerMiles, @Type)
            ON CONFLICT(Id) DO UPDATE SET
                VehicleId = excluded.VehicleId,
                EndTime = excluded.EndTime,
                OdometerMiles = excluded.OdometerMiles,
                Type = excluded.Type;";

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", record.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@EndTime", record.EndTime);
            cmd.Parameters.AddWithValue("@OdometerMiles", record.OdometerMiles);
            cmd.Parameters.AddWithValue("@Type", type);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<(Guid Id, Guid VehicleId, DateTime EndTime, decimal OdometerMiles, string Type)>
            GetRange(SqliteConnection conn, Guid vehicleId, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT Id, VehicleId, EndTime, OdometerMiles, Type
            FROM MileageRecords
            WHERE VehicleId = @VehicleId AND EndTime >= @Start AND EndTime <= @End
            ORDER BY EndTime;";
            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return (
                    Guid.Parse(reader.GetString(0)),
                    Guid.Parse(reader.GetString(1)),
                    reader.GetDateTime(2),
                    (decimal)(double)reader.GetDouble(3),
                    reader.GetString(4)
                );
            }
        }

        public (Guid VehicleId, DateTime EndTime, decimal OdometerMiles, string Type)?
            GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT VehicleId, EndTime, OdometerMiles, Type
            FROM MileageRecords
            WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                Guid.Parse(reader.GetString(0)),
                reader.GetDateTime(1),
                (decimal)(double)reader.GetDouble(2),
                reader.GetString(3)
            );
        }

        public (Guid Id, Guid VehicleId, DateTime EndTime, decimal OdometerMiles)?
            GetLatestByType(SqliteConnection conn, string type)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT Id, VehicleId, EndTime, OdometerMiles
            FROM MileageRecords
            WHERE Type = @Type
            ORDER BY EndTime DESC
            LIMIT 1;";
            cmd.Parameters.AddWithValue("@Type", type);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetDateTime(2),
                (decimal)(double)reader.GetDouble(3)
            );
        }

        public void Update(SqliteConnection conn, MileageRecordBase record)
        {
            using var cmd = conn.CreateCommand();

            cmd.CommandText =
            @"
        UPDATE MileageRecords
        SET
            VehicleId = @VehicleId,
            OdometerMiles = @OdometerMiles,
            EndTime = @EndTime
        WHERE Id = @Id;
    ";

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", record.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@OdometerMiles", record.OdometerMiles);
            cmd.Parameters.AddWithValue("@EndTime", record.EndTime);

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------
        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM MileageRecords WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
