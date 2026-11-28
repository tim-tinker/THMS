using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Income;

namespace THMS.Data.Stores.SqlTables
{
    public class FormW2sTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS FormW2s (
                    Id TEXT PRIMARY KEY,
                    TaxYear INTEGER NOT NULL,
                    Employer TEXT NOT NULL,
                    Wages REAL NOT NULL,
                    SocialSecurityWages REAL NOT NULL,
                    Traditional401k REAL NOT NULL,
                    Roth401k REAL NOT NULL,
                    Hsa REAL NOT NULL,
                    CreatedOn TEXT NOT NULL,
                    ModifiedOn TEXT NOT NULL,
                    PersonId TEXT NOT NULL DEFAULT ''
                );";
            cmd.ExecuteNonQuery();
            SqliteSchema.EnsureColumn(conn, "FormW2s", "PersonId", "TEXT NOT NULL DEFAULT ''");
        }

        public void Upsert(SqliteConnection conn, FormW2 form)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO FormW2s (
                    Id, TaxYear, Employer, Wages, SocialSecurityWages,
                    Traditional401k, Roth401k, Hsa, CreatedOn, ModifiedOn, PersonId)
                VALUES (
                    @Id, @TaxYear, @Employer, @Wages, @SocialSecurityWages,
                    @Traditional401k, @Roth401k, @Hsa, @CreatedOn, @ModifiedOn, @PersonId)
                ON CONFLICT(Id) DO UPDATE SET
                    TaxYear = excluded.TaxYear,
                    Employer = excluded.Employer,
                    Wages = excluded.Wages,
                    SocialSecurityWages = excluded.SocialSecurityWages,
                    Traditional401k = excluded.Traditional401k,
                    Roth401k = excluded.Roth401k,
                    Hsa = excluded.Hsa,
                    ModifiedOn = excluded.ModifiedOn,
                    PersonId = excluded.PersonId;";
            Bind(cmd, form);
            cmd.ExecuteNonQuery();
        }

        public List<FormW2> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, TaxYear, Employer, Wages, SocialSecurityWages,
                    Traditional401k, Roth401k, Hsa, CreatedOn, ModifiedOn, PersonId
                FROM FormW2s
                ORDER BY TaxYear DESC, Employer COLLATE NOCASE ASC, CreatedOn DESC;";
            var forms = new List<FormW2>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                forms.Add(Read(reader));
            return forms;
        }

        public FormW2? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, TaxYear, Employer, Wages, SocialSecurityWages,
                    Traditional401k, Roth401k, Hsa, CreatedOn, ModifiedOn, PersonId
                FROM FormW2s
                WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM FormW2s WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void Bind(SqliteCommand cmd, FormW2 form)
        {
            cmd.Parameters.AddWithValue("@Id", form.Id.ToString());
            cmd.Parameters.AddWithValue("@TaxYear", form.TaxYear);
            cmd.Parameters.AddWithValue("@Employer", form.Employer);
            cmd.Parameters.AddWithValue("@Wages", form.Wages);
            cmd.Parameters.AddWithValue("@SocialSecurityWages", form.SocialSecurityWages);
            cmd.Parameters.AddWithValue("@Traditional401k", form.Traditional401k);
            cmd.Parameters.AddWithValue("@Roth401k", form.Roth401k);
            cmd.Parameters.AddWithValue("@Hsa", form.Hsa);
            cmd.Parameters.AddWithValue("@CreatedOn", form.CreatedOn);
            cmd.Parameters.AddWithValue("@ModifiedOn", form.ModifiedOn);
            cmd.Parameters.AddWithValue("@PersonId", form.PersonId == Guid.Empty ? "" : form.PersonId.ToString());
        }

        private static FormW2 Read(SqliteDataReader reader) => new()
        {
            Id = Guid.Parse(reader.GetString(0)),
            TaxYear = reader.GetInt32(1),
            Employer = reader.GetString(2),
            Wages = Money(reader, 3),
            SocialSecurityWages = Money(reader, 4),
            Traditional401k = Money(reader, 5),
            Roth401k = Money(reader, 6),
            Hsa = Money(reader, 7),
            CreatedOn = reader.GetDateTime(8),
            ModifiedOn = reader.GetDateTime(9),
            PersonId = ReadPerson(reader, 10)
        };

        private static decimal Money(SqliteDataReader reader, int ordinal) =>
            (decimal)(double)reader.GetDouble(ordinal);

        private static Guid ReadPerson(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return Guid.Empty;
            var text = reader.GetString(ordinal);
            return Guid.TryParse(text, out var id) ? id : Guid.Empty;
        }
    }
}
