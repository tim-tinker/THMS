namespace THMS.Domain.Finance.Investments
{
    public sealed class InvestmentHistoryColumnMap
    {
        public Guid AccountId { get; set; }
        public string DateColumn { get; set; } = "";
        public string ActionColumn { get; set; } = "";
        public string SymbolColumn { get; set; } = "";
        public string DescriptionColumn { get; set; } = "";
        public string QuantityColumn { get; set; } = "";
        public string PriceColumn { get; set; } = "";
        public string AmountColumn { get; set; } = "";
        public string CashBalanceColumn { get; set; } = "";

        public InvestmentHistoryColumnMap ForAccount(Guid accountId)
        {
            var copy = (InvestmentHistoryColumnMap)MemberwiseClone();
            copy.AccountId = accountId;
            return copy;
        }
    }
}
