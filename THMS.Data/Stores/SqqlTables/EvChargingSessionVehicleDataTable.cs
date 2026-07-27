using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

public class EvChargingSessionVehicleDataTable
{
    public void InitializeSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS EvChargingSessionVehicleData (
                Id TEXT PRIMARY KEY,
                StartTimestamp TEXT,
                StartSocPercent INTEGER,
                EndSocPercent INTEGER
            );";
        cmd.ExecuteNonQuery();
    }

    public void Insert(SqliteConnection conn, EvChargingSessionVehicleData data)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO EvChargingSessionVehicleData
            (Id, StartTimestamp, StartSocPercent, EndSocPercent)
            VALUES
            (@Id, @StartTimestamp, @StartSocPercent, @EndSocPercent);";

        cmd.Parameters.AddWithValue("@Id", data.Id.ToString());
        cmd.Parameters.AddWithValue("@StartTimestamp", (object?)data.StartTimestamp ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartSocPercent", (object?)data.StartSocPercent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EndSocPercent", (object?)data.EndSocPercent ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public void Update(SqliteConnection conn, EvChargingSessionVehicleData data)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE EvChargingSessionVehicleData
            SET StartTimestamp = @StartTimestamp,
                StartSocPercent = @StartSocPercent,
                EndSocPercent = @EndSocPercent
            WHERE Id = @Id;";

        cmd.Parameters.AddWithValue("@Id", data.Id.ToString());
        cmd.Parameters.AddWithValue("@StartTimestamp", (object?)data.StartTimestamp ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartSocPercent", (object?)data.StartSocPercent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EndSocPercent", (object?)data.EndSocPercent ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public (DateTime? StartTimestamp, int? StartSocPercent, int? EndSocPercent)? GetById(SqliteConnection conn, Guid id)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT StartTimestamp, StartSocPercent, EndSocPercent
            FROM EvChargingSessionVehicleData
            WHERE Id = @Id;";
        cmd.Parameters.AddWithValue("@Id", id.ToString());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return (
            reader.IsDBNull(0) ? null : reader.GetDateTime(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetInt32(2)
        );
    }
}
