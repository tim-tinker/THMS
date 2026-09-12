using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class StatementChargeLinesTable
    {
        public const string UtilityKind = "Utility";
        public const string ServiceKind = "Service";

        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS StatementChargeLines (
                    Id TEXT PRIMARY KEY,
                    StatementId TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Kind TEXT NOT NULL,
                    SortOrder INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS IX_StatementChargeLines_StatementId
                    ON StatementChargeLines (StatementId);";
            cmd.ExecuteNonQuery();
        }

        public void ReplaceUtility(SqliteConnection conn, Guid statementId, IEnumerable<UtilityChargeLine> charges) =>
            ReplaceAll(
                conn,
                statementId,
                UtilityKind,
                charges.Select(c => (c.Description, c.Amount)));

        public void ReplaceService(SqliteConnection conn, Guid statementId, IEnumerable<ServiceChargeLine> charges) =>
            ReplaceAll(
                conn,
                statementId,
                ServiceKind,
                charges.Select(c => (c.Description, c.Amount)));

        public void DeleteByStatement(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM StatementChargeLines WHERE StatementId = @StatementId;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.ExecuteNonQuery();
        }

        public List<UtilityChargeLine> GetUtility(SqliteConnection conn, Guid statementId) =>
            GetLines(conn, statementId, UtilityKind)
                .Select(line => new UtilityChargeLine { Description = line.Description, Amount = line.Amount })
                .ToList();

        public List<ServiceChargeLine> GetService(SqliteConnection conn, Guid statementId) =>
            GetLines(conn, statementId, ServiceKind)
                .Select(line => new ServiceChargeLine { Description = line.Description, Amount = line.Amount })
                .ToList();

        private void ReplaceAll(
            SqliteConnection conn,
            Guid statementId,
            string kind,
            IEnumerable<(string Description, decimal Amount)> charges)
        {
            DeleteByStatement(conn, statementId);
            var order = 0;
            foreach (var charge in charges)
                Insert(conn, statementId, kind, charge.Description, charge.Amount, order++);
        }

        private static List<(string Description, decimal Amount)> GetLines(
            SqliteConnection conn,
            Guid statementId,
            string kind)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Description, Amount
                FROM StatementChargeLines
                WHERE StatementId = @StatementId AND Kind = @Kind
                ORDER BY SortOrder, Id;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.Parameters.AddWithValue("@Kind", kind);
            using var reader = cmd.ExecuteReader();
            var list = new List<(string Description, decimal Amount)>();
            while (reader.Read())
            {
                list.Add((
                    reader.IsDBNull(0) ? "" : reader.GetString(0),
                    (decimal)(double)reader.GetDouble(1)));
            }

            return list;
        }

        private static void Insert(
            SqliteConnection conn,
            Guid statementId,
            string kind,
            string description,
            decimal amount,
            int sortOrder)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO StatementChargeLines
                (Id, StatementId, Description, Amount, Kind, SortOrder)
                VALUES
                (@Id, @StatementId, @Description, @Amount, @Kind, @SortOrder);";
            cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@Kind", kind);
            cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            cmd.ExecuteNonQuery();
        }
    }
}
