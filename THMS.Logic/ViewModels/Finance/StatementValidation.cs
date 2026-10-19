namespace THMS.Logic.ViewModels.Finance
{
    public sealed class StatementValidation
    {
        public DateTime PreviousStatementDate { get; init; }
        public decimal OpeningBalance { get; init; }
        public DateTime StatementDate { get; init; }
        public decimal StatementBalance { get; init; }
        public IReadOnlyList<StatementValidationRow> Rows { get; init; } = [];
        public decimal EndingBalance { get; init; }

        public decimal Difference => EndingBalance - StatementBalance;
    }

    public sealed class StatementValidationRow
    {
        public DateTime Date { get; init; }
        public string Description { get; init; } = "";
        public decimal Amount { get; init; }
        public decimal Balance { get; init; }
    }
}
