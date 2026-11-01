namespace THMS.Domain.Finance.Investments
{
    public sealed class InvestmentAction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public int Sequence { get; set; }
        public DateTime RunDate { get; set; }
        public string ActionText { get; set; } = "";
        public string Symbol { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal? Amount { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? SplitFactor { get; set; }
        public decimal? CashChange { get; set; }
        public decimal? ShareChange { get; set; }
        public InvestmentActionKind Kind { get; set; }
        public InvestmentActionStatus Status { get; set; }
        public string ImportKey { get; set; } = "";
        public string? CashBalanceText { get; set; }
        public decimal? FileCashBalance { get; set; }
        public decimal? BookCashAfter { get; set; }
        public string? Note { get; set; }
        public Guid? LedgerTransactionId { get; set; }
    }
}
