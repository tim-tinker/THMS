namespace THMS.Logic.ViewModels.Assets
{
    public sealed class AssetListRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
        public string Kind { get; init; } = "";
        public string Category { get; init; } = "";
        public string AcquiredOn { get; init; } = "";
        public decimal CostBasis { get; init; }
        public string LatestValue { get; init; } = "";
        public string SecuredAccount { get; init; } = "";
    }
}
