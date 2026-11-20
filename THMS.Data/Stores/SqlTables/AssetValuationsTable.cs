using Microsoft.Data.Sqlite;
using THMS.Domain.Assets;

namespace THMS.Data.Stores.SqlTables
{
    public class AssetValuationsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS AssetValuations (
                    Id TEXT PRIMARY KEY,
                    AssetId TEXT NOT NULL,
                    ValueType TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    AsOf TEXT NOT NULL,
                    Source TEXT NOT NULL,
                    SourceStatementId TEXT,
                    AppraisalId TEXT,
                    AssessmentRatioPercent REAL NOT NULL DEFAULT 100,
                    CreatedOn TEXT NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_AssetValuations_AssetId
                    ON AssetValuations (AssetId);
                CREATE INDEX IF NOT EXISTS IX_AssetValuations_SourceStatementId
                    ON AssetValuations (SourceStatementId);";
            cmd.ExecuteNonQuery();
            SqliteSchema.EnsureColumn(conn, "AssetValuations", "AppraisalId", "TEXT");
            SqliteSchema.EnsureColumn(conn, "AssetValuations", "AssessmentRatioPercent", "REAL NOT NULL DEFAULT 100");
        }

        public void Upsert(SqliteConnection conn, AssetValuation valuation)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AssetValuations
                    (Id, AssetId, ValueType, Amount, AsOf, Source, SourceStatementId, AppraisalId, AssessmentRatioPercent, CreatedOn, ModifiedOn)
                VALUES
                    (@Id, @AssetId, @ValueType, @Amount, @AsOf, @Source, @SourceStatementId, @AppraisalId, @Ratio, @CreatedOn, @ModifiedOn)
                ON CONFLICT(Id) DO UPDATE SET
                    AssetId = excluded.AssetId,
                    ValueType = excluded.ValueType,
                    Amount = excluded.Amount,
                    AsOf = excluded.AsOf,
                    Source = excluded.Source,
                    SourceStatementId = excluded.SourceStatementId,
                    AppraisalId = excluded.AppraisalId,
                    AssessmentRatioPercent = excluded.AssessmentRatioPercent,
                    ModifiedOn = excluded.ModifiedOn;";
            Bind(cmd, valuation);
            cmd.ExecuteNonQuery();
        }

        public List<AssetValuation> GetForAsset(SqliteConnection conn, Guid assetId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = Select + @"
                WHERE AssetId = @AssetId
                ORDER BY AsOf DESC, ValueType;";
            cmd.Parameters.AddWithValue("@AssetId", assetId.ToString());
            return ReadAll(cmd);
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM AssetValuations WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public void DeleteForAsset(SqliteConnection conn, Guid assetId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM AssetValuations WHERE AssetId = @AssetId;";
            cmd.Parameters.AddWithValue("@AssetId", assetId.ToString());
            cmd.ExecuteNonQuery();
        }

        public void DeleteForStatement(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM AssetValuations WHERE SourceStatementId = @StatementId;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.ExecuteNonQuery();
        }

        private const string Select = @"
            SELECT Id, AssetId, ValueType, Amount, AsOf, Source, SourceStatementId, CreatedOn, ModifiedOn,
                   AppraisalId, AssessmentRatioPercent
            FROM AssetValuations";

        private static void Bind(SqliteCommand cmd, AssetValuation valuation)
        {
            cmd.Parameters.AddWithValue("@Id", valuation.Id.ToString());
            cmd.Parameters.AddWithValue("@AssetId", valuation.AssetId.ToString());
            cmd.Parameters.AddWithValue("@ValueType", valuation.ValueType);
            cmd.Parameters.AddWithValue("@Amount", valuation.Amount);
            cmd.Parameters.AddWithValue("@AsOf", valuation.AsOf);
            cmd.Parameters.AddWithValue("@Source", valuation.Source ?? "");
            cmd.Parameters.AddWithValue(
                "@SourceStatementId",
                valuation.SourceStatementId is Guid source && source != Guid.Empty
                    ? source.ToString()
                    : DBNull.Value);
            cmd.Parameters.AddWithValue(
                "@AppraisalId",
                valuation.AppraisalId != Guid.Empty ? valuation.AppraisalId.ToString() : DBNull.Value);
            cmd.Parameters.AddWithValue("@Ratio", valuation.AssessmentRatioPercent);
            cmd.Parameters.AddWithValue("@CreatedOn", valuation.CreatedOn);
            cmd.Parameters.AddWithValue("@ModifiedOn", valuation.ModifiedOn);
        }

        private static List<AssetValuation> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<AssetValuation>();
            while (reader.Read())
            {
                list.Add(new AssetValuation
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    AssetId = Guid.Parse(reader.GetString(1)),
                    ValueType = reader.GetString(2),
                    Amount = (decimal)(double)reader.GetDouble(3),
                    AsOf = reader.GetDateTime(4),
                    Source = reader.GetString(5),
                    SourceStatementId = reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6)),
                    CreatedOn = reader.GetDateTime(7),
                    ModifiedOn = reader.GetDateTime(8),
                    AppraisalId = reader.FieldCount > 9 && !reader.IsDBNull(9) && Guid.TryParse(reader.GetString(9), out var appraisal)
                        ? appraisal
                        : Guid.Empty,
                    AssessmentRatioPercent = reader.FieldCount > 10 && !reader.IsDBNull(10)
                        ? (decimal)(double)reader.GetDouble(10)
                        : 100m
                });
            }

            return list;
        }
    }
}
