namespace THMS.Domain.Finance.Investments
{
    public sealed class InvestmentFileSheet
    {
        public IReadOnlyList<string> Headers { get; init; } = [];
        public IReadOnlyList<InvestmentFileRawRow> Rows { get; init; } = [];
    }

    public sealed class InvestmentFileRawRow
    {
        public int SourceRow { get; init; }
        public IReadOnlyList<string> Cells { get; init; } = [];
    }
}
