using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

public class MileageRecordTable
{
    public void InitializeSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS MileageRecords (
                Id TEXT PRIMARY KEY,
                VehicleId TEXT NOT NULL,
                Date TEXT NOT NULL,
                OdometerMiles REAL NOT NULL,
                Notes TEXT,
                Type TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    public void Insert(SqliteConnection conn, MileageRecordBase record, string type)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO MileageRecords
            (Id, VehicleId, Date, OdometerMiles, Notes, Type)
            VALUES
            (@Id, @VehicleId, @Date, @OdometerMiles, @Notes, @Type);";

        cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
        cmd.Parameters.AddWithValue("@VehicleId", record.VehicleId.ToString());
        cmd.Parameters.AddWithValue("@Date", record.Date);
        cmd.Parameters.AddWithValue("@OdometerMiles", record.OdometerMiles);
        cmd.Parameters.AddWithValue("@Notes", (object?)record.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Type", type);

        cmd.ExecuteNonQuery();
    }

    public IEnumerable<(Guid Id, Guid VehicleId, DateTime Date, decimal OdometerMiles, string? Notes, string Type)>
        GetRange(SqliteConnection conn, Guid vehicleId, DateTime start, DateTime end)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, VehicleId, Date, OdometerMiles, Notes, Type
            FROM MileageRecords
            WHERE VehicleId = @VehicleId AND Date >= @Start AND Date <= @End
            ORDER BY Date;";
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
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(5)
            );
        }
    }

    public (Guid VehicleId, DateTime Date, decimal OdometerMiles, string? Notes, string Type)?
        GetById(SqliteConnection conn, Guid id)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT VehicleId, Date, OdometerMiles, Notes, Type
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
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetString(4)
        );
    }
}
