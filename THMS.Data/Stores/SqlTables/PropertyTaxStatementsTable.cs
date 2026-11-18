using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class PropertyTaxStatementsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PropertyTaxStatements (
                    StatementId TEXT PRIMARY KEY,
                    AssetId TEXT NOT NULL,
                    TaxYear INTEGER NOT NULL,
                    LandMarketValue REAL NOT NULL,
                    ImprovementMarketValue REAL NOT NULL,
                    TotalMarketValue REAL NOT NULL,
                    HomesteadCap REAL NOT NULL,
                    AssessedValue REAL NOT NULL,
                    Exemptions TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_PropertyTaxStatements_AssetId
                    ON PropertyTaxStatements (AssetId);
                CREATE TABLE IF NOT EXISTS PropertyTaxJurisdictionLines (
                    StatementId TEXT NOT NULL,
                    LineOrder INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    ExemptionAmount REAL NOT NULL,
                    TaxableValue REAL NOT NULL,
                    RatePerHundred REAL NOT NULL,
                    TaxAmount REAL NOT NULL,
                    PRIMARY KEY (StatementId, LineOrder)
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, PropertyTaxStatement statement)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO PropertyTaxStatements
                        (StatementId, AssetId, TaxYear, LandMarketValue, ImprovementMarketValue,
                         TotalMarketValue, HomesteadCap, AssessedValue, Exemptions)
                    VALUES
                        (@Id, @AssetId, @TaxYear, @Land, @Improvement, @Total, @Cap, @Assessed, @Exemptions)
                    ON CONFLICT(StatementId) DO UPDATE SET
                        AssetId = excluded.AssetId,
                        TaxYear = excluded.TaxYear,
                        LandMarketValue = excluded.LandMarketValue,
                        ImprovementMarketValue = excluded.ImprovementMarketValue,
                        TotalMarketValue = excluded.TotalMarketValue,
                        HomesteadCap = excluded.HomesteadCap,
                        AssessedValue = excluded.AssessedValue,
                        Exemptions = excluded.Exemptions;";
                cmd.Parameters.AddWithValue("@Id", statement.Id.ToString());
                cmd.Parameters.AddWithValue("@AssetId", statement.AssetId.ToString());
                cmd.Parameters.AddWithValue("@TaxYear", statement.TaxYear);
                cmd.Parameters.AddWithValue("@Land", statement.LandMarketValue);
                cmd.Parameters.AddWithValue("@Improvement", statement.ImprovementMarketValue);
                cmd.Parameters.AddWithValue("@Total", statement.TotalMarketValue);
                cmd.Parameters.AddWithValue("@Cap", statement.HomesteadCap);
                cmd.Parameters.AddWithValue("@Assessed", statement.AssessedValue);
                cmd.Parameters.AddWithValue("@Exemptions", statement.Exemptions ?? "");
                cmd.ExecuteNonQuery();
            }

            DeleteLines(conn, statement.Id);
            var order = 0;
            foreach (var line in statement.Jurisdictions)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO PropertyTaxJurisdictionLines
                        (StatementId, LineOrder, Name, ExemptionAmount, TaxableValue, RatePerHundred, TaxAmount)
                    VALUES
                        (@Id, @Order, @Name, @Exemption, @Taxable, @Rate, @Tax);";
                cmd.Parameters.AddWithValue("@Id", statement.Id.ToString());
                cmd.Parameters.AddWithValue("@Order", order++);
                cmd.Parameters.AddWithValue("@Name", line.Name ?? "");
                cmd.Parameters.AddWithValue("@Exemption", line.ExemptionAmount);
                cmd.Parameters.AddWithValue("@Taxable", line.TaxableValue);
                cmd.Parameters.AddWithValue("@Rate", line.RatePerHundred);
                cmd.Parameters.AddWithValue("@Tax", line.TaxAmount);
                cmd.ExecuteNonQuery();
            }
        }

        public PropertyTaxStatement Load(SqliteConnection conn, Guid statementId)
        {
            var statement = new PropertyTaxStatement();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT AssetId, TaxYear, LandMarketValue, ImprovementMarketValue, TotalMarketValue,
                           HomesteadCap, AssessedValue, Exemptions
                    FROM PropertyTaxStatements
                    WHERE StatementId = @Id;";
                cmd.Parameters.AddWithValue("@Id", statementId.ToString());
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    statement.AssetId = Guid.Parse(reader.GetString(0));
                    statement.TaxYear = reader.GetInt32(1);
                    statement.LandMarketValue = Money(reader, 2);
                    statement.ImprovementMarketValue = Money(reader, 3);
                    statement.TotalMarketValue = Money(reader, 4);
                    statement.HomesteadCap = Money(reader, 5);
                    statement.AssessedValue = Money(reader, 6);
                    statement.Exemptions = reader.GetString(7);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Name, ExemptionAmount, TaxableValue, RatePerHundred, TaxAmount
                    FROM PropertyTaxJurisdictionLines
                    WHERE StatementId = @Id
                    ORDER BY LineOrder;";
                cmd.Parameters.AddWithValue("@Id", statementId.ToString());
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    statement.Jurisdictions.Add(new PropertyTaxJurisdictionLine
                    {
                        Name = reader.GetString(0),
                        ExemptionAmount = Money(reader, 1),
                        TaxableValue = Money(reader, 2),
                        RatePerHundred = Money(reader, 3),
                        TaxAmount = Money(reader, 4)
                    });
                }
            }

            return statement;
        }

        public List<Guid> GetStatementIds(SqliteConnection conn, Guid assetId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT StatementId FROM PropertyTaxStatements WHERE AssetId = @AssetId;";
            cmd.Parameters.AddWithValue("@AssetId", assetId.ToString());
            using var reader = cmd.ExecuteReader();
            var ids = new List<Guid>();
            while (reader.Read())
                ids.Add(Guid.Parse(reader.GetString(0)));
            return ids;
        }

        public void Delete(SqliteConnection conn, Guid statementId)
        {
            DeleteLines(conn, statementId);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM PropertyTaxStatements WHERE StatementId = @Id;";
            cmd.Parameters.AddWithValue("@Id", statementId.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void DeleteLines(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM PropertyTaxJurisdictionLines WHERE StatementId = @Id;";
            cmd.Parameters.AddWithValue("@Id", statementId.ToString());
            cmd.ExecuteNonQuery();
        }

        private static decimal Money(SqliteDataReader reader, int ordinal) =>
            (decimal)(double)reader.GetDouble(ordinal);
    }
}
