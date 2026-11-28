using Microsoft.Data.Sqlite;
using THMS.Domain.Household;

namespace THMS.Data.Stores.SqlTables
{
    public class HouseholdPersonTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS HouseholdPerson (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    BirthYear INTEGER NOT NULL,
                    CreatedOn TEXT NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, HouseholdPerson person)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO HouseholdPerson (Id, Name, BirthYear, CreatedOn, ModifiedOn)
                VALUES (@Id, @Name, @BirthYear, @CreatedOn, @ModifiedOn)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = excluded.Name,
                    BirthYear = excluded.BirthYear,
                    ModifiedOn = excluded.ModifiedOn;";
            cmd.Parameters.AddWithValue("@Id", person.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", person.Name);
            cmd.Parameters.AddWithValue("@BirthYear", person.BirthYear);
            cmd.Parameters.AddWithValue("@CreatedOn", person.CreatedOn);
            cmd.Parameters.AddWithValue("@ModifiedOn", person.ModifiedOn);
            cmd.ExecuteNonQuery();
        }

        public HouseholdPerson? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, BirthYear, CreatedOn, ModifiedOn
                FROM HouseholdPerson
                WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public List<HouseholdPerson> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, BirthYear, CreatedOn, ModifiedOn
                FROM HouseholdPerson
                ORDER BY Name COLLATE NOCASE ASC;";
            var people = new List<HouseholdPerson>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                people.Add(Read(reader));
            return people;
        }

        private static HouseholdPerson Read(SqliteDataReader reader) => new()
        {
            Id = Guid.Parse(reader.GetString(0)),
            Name = reader.GetString(1),
            BirthYear = reader.GetInt32(2),
            CreatedOn = reader.GetDateTime(3),
            ModifiedOn = reader.GetDateTime(4)
        };
    }
}
