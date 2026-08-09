using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class IceMileageTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS IceMileageRecords (
                Id TEXT PRIMARY KEY,
                GallonsAdded REAL NOT NULL,
                IsFullFillUp INTEGER NOT NULL,
                FuelCost REAL NOT NULL
            );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, IceMileageRecord record)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO IceMileageRecords
            (Id, GallonsAdded, IsFullFillUp, FuelCost)
            VALUES
            (@Id, @GallonsAdded, @IsFullFillUp, @FuelCost)
            ON CONFLICT(Id) DO UPDATE SET
                GallonsAdded = excluded.GallonsAdded,
                IsFullFillUp = excluded.IsFullFillUp,
                FuelCost = excluded.FuelCost;";

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@GallonsAdded", record.GallonsAdded);
            cmd.Parameters.AddWithValue("@IsFullFillUp", record.IsFullFillUp ? 1 : 0);
            cmd.Parameters.AddWithValue("@FuelCost", record.FuelCost);

            cmd.ExecuteNonQuery();
        }

        public (decimal GallonsAdded, bool IsFullFillUp, decimal FuelCost)? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT GallonsAdded, IsFullFillUp, FuelCost
            FROM IceMileageRecords
            WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                reader.GetInt32(1) == 1,
                (decimal)(double)reader.GetDouble(2)
            );
        }
    }
}
