namespace THMS.Logic.ViewModels.Income
{
    public sealed class IncomeEmployerTab
    {
        public string Key { get; init; } = "";
        public string Caption { get; init; } = "";
        public IReadOnlyList<IncomeYearRow> Years { get; init; } = [];
        public IReadOnlyList<Guid> PayslipIds { get; init; } = [];
    }

    /// <summary>
    /// One year on an employer tab. A placeholder stands in until that year's W-2 is filed.
    /// </summary>
    public sealed class IncomeYearRow
    {
        public Guid? FormId { get; init; }
        public int Year { get; init; }
        public string Record { get; init; } = "";
        public decimal? Wages { get; init; }
        public decimal? SocialSecurityWages { get; init; }
        public decimal? Traditional401k { get; init; }
        public decimal? Roth401k { get; init; }
        public decimal? Hsa { get; init; }
        public decimal? GrossSoFar { get; init; }

        public bool IsPlaceholder => FormId is null;
        public string WagesText => Money(Wages);
        public string SocialSecurityWagesText => Money(SocialSecurityWages);
        public string Traditional401kText => Money(Traditional401k);
        public string Roth401kText => Money(Roth401k);
        public string HsaText => Money(Hsa);
        public string GrossSoFarText => Money(GrossSoFar);

        private static string Money(decimal? amount) => amount?.ToString("c2") ?? "";
    }
}
