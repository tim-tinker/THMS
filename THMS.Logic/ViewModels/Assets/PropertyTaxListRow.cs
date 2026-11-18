namespace THMS.Logic.ViewModels.Assets
{
    public sealed class PropertyTaxListRow
    {
        public Guid Id { get; init; }
        public int TaxYear { get; init; }
        public DateTime ProcessDate { get; init; }
        public DateTime DueDate { get; init; }
        public decimal AmountDue { get; init; }
        public string Collector { get; init; } = "";
    }
}
