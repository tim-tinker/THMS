namespace THMS.Domain.Finance.Planning
{
    /// <summary>Birth year used to figure age at the end of a tax year.</summary>
    public sealed record Retirement401kBirthYear
    {
        public int Year { get; init; }
    }
}
