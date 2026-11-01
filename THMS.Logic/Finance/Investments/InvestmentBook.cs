using THMS.Domain.Finance.Investments;

namespace THMS.Logic.Finance.Investments
{
    public sealed class InvestmentBookState
    {
        public Guid AccountId { get; init; }
        public DateTime? OpenedOn { get; set; }
        public List<InvestmentAction> Actions { get; } = [];
        public List<SecurityQuote> Quotes { get; } = [];
        public List<InvestmentSecurity> Securities { get; } = [];
    }

    public sealed class InvestmentPositionRow
    {
        public string Symbol { get; init; } = "";
        public string Name { get; init; } = "";
        public decimal Shares { get; init; }
        public decimal AverageCost { get; init; }
        public decimal TotalBasis { get; init; }
        public decimal? Price { get; init; }
        public decimal? MarketValue { get; init; }
        public decimal? EstimatedAnnualIncome { get; init; }
        public DateTime? ExDate { get; init; }
        public DateTime? PayDate { get; init; }
    }

    public sealed class InvestmentIncomePoint
    {
        public DateTime Date { get; init; }
        public decimal EstimatedAnnualIncome { get; init; }
    }

    public sealed class InvestmentSnapshot
    {
        public decimal Cash { get; init; }
        public decimal PositionValue { get; init; }
        public decimal AccountValue { get; init; }
        public decimal EstimatedAnnualIncome { get; init; }
        public IReadOnlyList<InvestmentPositionRow> Positions { get; init; } = [];
        public IReadOnlyList<InvestmentIncomePoint> Income { get; init; } = [];
        public IReadOnlyDictionary<Guid, decimal> CashAfter { get; init; } = new Dictionary<Guid, decimal>();
        public IReadOnlySet<Guid> Failed { get; init; } = new HashSet<Guid>();
    }

    public sealed class InvestmentImportPreview
    {
        public int Applied { get; init; }
        public int Skipped { get; init; }
        public int Updated { get; init; }
        public int Review { get; init; }
        public IReadOnlyList<string> Messages { get; init; } = [];
        public IReadOnlyList<InvestmentAction> Changes { get; init; } = [];
        public InvestmentSnapshot Snapshot { get; init; } = new();
    }
}
