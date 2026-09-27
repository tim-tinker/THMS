using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class PlaidItemSyncStateTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PlaidItemSyncState (
                    ItemId TEXT PRIMARY KEY,
                    Cursor TEXT NOT NULL DEFAULT '',
                    HistoryStartDate TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    LastSyncedAt TEXT,
                    LastError TEXT NOT NULL DEFAULT ''
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, PlaidItemSyncState state)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PlaidItemSyncState
                (ItemId, Cursor, HistoryStartDate, Status, LastSyncedAt, LastError)
                VALUES
                (@ItemId, @Cursor, @HistoryStartDate, @Status, @LastSyncedAt, @LastError)
                ON CONFLICT(ItemId) DO UPDATE SET
                    Cursor = excluded.Cursor,
                    HistoryStartDate = excluded.HistoryStartDate,
                    Status = excluded.Status,
                    LastSyncedAt = excluded.LastSyncedAt,
                    LastError = excluded.LastError;";
            cmd.Parameters.AddWithValue("@ItemId", state.ItemId);
            cmd.Parameters.AddWithValue("@Cursor", state.Cursor ?? "");
            cmd.Parameters.AddWithValue("@HistoryStartDate", BindDate(state.HistoryStartDate));
            cmd.Parameters.AddWithValue("@Status", (int)state.Status);
            cmd.Parameters.AddWithValue("@LastSyncedAt", BindDate(state.LastSyncedAt));
            cmd.Parameters.AddWithValue("@LastError", state.LastError ?? "");
            cmd.ExecuteNonQuery();
        }

        public PlaidItemSyncState? Get(SqliteConnection conn, string itemId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM PlaidItemSyncState WHERE ItemId = @ItemId;";
            cmd.Parameters.AddWithValue("@ItemId", itemId);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IReadOnlyList<PlaidItemSyncState> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM PlaidItemSyncState ORDER BY ItemId;";
            using var reader = cmd.ExecuteReader();
            var list = new List<PlaidItemSyncState>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private const string SelectColumns =
            "SELECT ItemId, Cursor, HistoryStartDate, Status, LastSyncedAt, LastError";

        private static PlaidItemSyncState Read(SqliteDataReader reader) =>
            new()
            {
                ItemId = reader.GetString(0),
                Cursor = reader.IsDBNull(1) ? "" : reader.GetString(1),
                HistoryStartDate = ReadDate(reader, 2),
                Status = reader.IsDBNull(3) ? PlaidItemSyncStatus.NotSynced : (PlaidItemSyncStatus)reader.GetInt32(3),
                LastSyncedAt = ReadDate(reader, 4),
                LastError = reader.IsDBNull(5) ? "" : reader.GetString(5)
            };

        private static object BindDate(DateTime? value) =>
            value is DateTime date ? date.ToString("o") : DBNull.Value;

        private static DateTime? ReadDate(SqliteDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return null;
            var text = reader.GetString(index);
            return DateTime.TryParse(text, out var date) ? date : null;
        }
    }
}
