using Microsoft.Data.Sqlite;
using THMS.Domain.Assets;

namespace THMS.Data.Stores.SqlTables
{
    public class ValuationSourcesTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ValuationSources (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    WebsiteUrl TEXT NOT NULL,
                    Kinds INTEGER NOT NULL,
                    CreatedOn TEXT NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, ValuationSource source)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ValuationSources (Id, Name, WebsiteUrl, Kinds, CreatedOn, ModifiedOn)
                VALUES (@Id, @Name, @WebsiteUrl, @Kinds, @CreatedOn, @ModifiedOn)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = excluded.Name,
                    WebsiteUrl = excluded.WebsiteUrl,
                    Kinds = excluded.Kinds,
                    ModifiedOn = excluded.ModifiedOn;";
            cmd.Parameters.AddWithValue("@Id", source.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", source.Name);
            cmd.Parameters.AddWithValue("@WebsiteUrl", source.WebsiteUrl ?? "");
            cmd.Parameters.AddWithValue("@Kinds", (int)source.Kinds);
            cmd.Parameters.AddWithValue("@CreatedOn", source.CreatedOn);
            cmd.Parameters.AddWithValue("@ModifiedOn", source.ModifiedOn);
            cmd.ExecuteNonQuery();
        }

        public List<ValuationSource> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, WebsiteUrl, Kinds, CreatedOn, ModifiedOn
                FROM ValuationSources
                ORDER BY Name COLLATE NOCASE;";
            var sources = new List<ValuationSource>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sources.Add(new ValuationSource
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(1),
                    WebsiteUrl = reader.GetString(2),
                    Kinds = (AppraisedAssetKind)reader.GetInt32(3),
                    CreatedOn = reader.GetDateTime(4),
                    ModifiedOn = reader.GetDateTime(5)
                });
            }

            return sources;
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ValuationSources WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
