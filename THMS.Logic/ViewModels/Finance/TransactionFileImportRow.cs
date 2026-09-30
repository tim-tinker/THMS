namespace THMS.Logic.ViewModels.Finance
{
    public sealed class TransactionFileImportRow
    {
        public bool Import { get; set; }
        public int SourceRow { get; init; }
        public DateTime? Date { get; init; }
        public string Description { get; init; } = "";
        public decimal? Amount { get; init; }
        public string? Problem { get; init; }
        public bool IsDuplicate { get; init; }

        public string DateText => Date?.ToString("d") ?? "";
        public string AmountText => Amount?.ToString("C2") ?? "";
        public string Status => Problem ?? (IsDuplicate ? "Duplicate" : "");
    }
}
