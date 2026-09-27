namespace THMS.Domain.Finance.Accounts
{
    public enum PlaidItemSyncStatus
    {
        NotSynced = 0,
        Syncing = 1,
        Synced = 2,
        NeedsAuth = 3
    }

    public class PlaidItemSyncState
    {
        public string ItemId { get; set; } = "";
        public string Cursor { get; set; } = "";
        public DateTime? HistoryStartDate { get; set; }
        public PlaidItemSyncStatus Status { get; set; } = PlaidItemSyncStatus.NotSynced;
        public DateTime? LastSyncedAt { get; set; }
        public string LastError { get; set; } = "";

        public bool HasCursor => !string.IsNullOrWhiteSpace(Cursor);

        public string DisplayLabel => Status switch
        {
            PlaidItemSyncStatus.Syncing => "Syncing",
            PlaidItemSyncStatus.Synced => "Synced",
            PlaidItemSyncStatus.NeedsAuth => "Needs auth",
            _ => HasCursor ? "Not synced" : "Not synced"
        };
    }
}
