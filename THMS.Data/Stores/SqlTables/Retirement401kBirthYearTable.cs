using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class Retirement401kBirthYearTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Retirement401kBirthYear (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    BirthYear INTEGER NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Retirement401kBirthYear birthYear)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Retirement401kBirthYear (Id, BirthYear, ModifiedOn)
                VALUES (1, @BirthYear, @ModifiedOn)
                ON CONFLICT(Id) DO UPDATE SET
                    BirthYear = excluded.BirthYear,
                    ModifiedOn = excluded.ModifiedOn;";
            cmd.Parameters.AddWithValue("@BirthYear", birthYear.Year);
            cmd.Parameters.AddWithValue("@ModifiedOn", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public Retirement401kBirthYear? Get(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT BirthYear FROM Retirement401kBirthYear WHERE Id = 1;";
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;
            return new Retirement401kBirthYear { Year = reader.GetInt32(0) };
        }
    }
}
