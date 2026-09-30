namespace THMS.Domain.Finance.Transactions
{
    public sealed class TransactionFileColumnMap
    {
        public Guid AccountId { get; set; }
        public string DateColumn { get; set; } = "";
        public string DescriptionColumn { get; set; } = "";
        public bool UseDebitCredit { get; set; }
        public string AmountColumn { get; set; } = "";
        public string DebitColumn { get; set; } = "";
        public string CreditColumn { get; set; } = "";
        public bool FlipSign { get; set; }
    }

    public sealed class TransactionFileSheet
    {
        public IReadOnlyList<string> Headers { get; init; } = [];
        public IReadOnlyList<TransactionFileRawRow> Rows { get; init; } = [];
    }

    public sealed class TransactionFileRawRow
    {
        public int SourceRow { get; init; }
        public IReadOnlyList<string> Cells { get; init; } = [];
    }
}
