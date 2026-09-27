namespace THMS.External
{
    public sealed class TransactionSyncPage
    {
        public List<TransactionDto> Added { get; init; } = [];
        public List<TransactionDto> Modified { get; init; } = [];
        public List<string> RemovedIds { get; init; } = [];
        public string NextCursor { get; init; } = "";
        public bool HasMore { get; init; }
    }
}
