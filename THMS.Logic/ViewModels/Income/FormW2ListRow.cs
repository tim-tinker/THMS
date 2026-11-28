namespace THMS.Logic.ViewModels.Income
{
    public sealed class FormW2ListRow
    {
        public Guid Id { get; init; }
        public int TaxYear { get; init; }
        public string Employer { get; init; } = "";
        public decimal Wages { get; init; }
        public decimal SocialSecurityWages { get; init; }
        public decimal Traditional401k { get; init; }
        public decimal Roth401k { get; init; }
        public decimal Hsa { get; init; }
    }
}
