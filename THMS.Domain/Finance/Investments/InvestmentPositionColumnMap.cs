namespace THMS.Domain.Finance.Investments
{
    public sealed class InvestmentPositionColumnMap
    {
        public Guid AccountId { get; set; }
        public string SymbolColumn { get; set; } = "";
        public string DescriptionColumn { get; set; } = "";
        public string QuantityColumn { get; set; } = "";
        public string PriceColumn { get; set; } = "";
        public string ValueColumn { get; set; } = "";
        public string CostBasisColumn { get; set; } = "";
        public string AverageCostColumn { get; set; } = "";
        public string AccountNumberColumn { get; set; } = "";
        public string AccountNameColumn { get; set; } = "";
        public string AsOfColumn { get; set; } = "";
        public string AmountPerShareColumn { get; set; } = "";
        public string EstimatedIncomeColumn { get; set; } = "";
        public string DistributionRateColumn { get; set; } = "";
        public string RateAsOfColumn { get; set; } = "";
        public string ExDateColumn { get; set; } = "";
        public string PayDateColumn { get; set; } = "";

        public InvestmentPositionColumnMap ForAccount(Guid accountId)
        {
            var copy = (InvestmentPositionColumnMap)MemberwiseClone();
            copy.AccountId = accountId;
            return copy;
        }
    }
}
