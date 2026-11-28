namespace THMS.Domain.Finance.Income
{
    /// <summary>
    /// One employer W-2. Deferred amounts are the box 12 codes the planning calculators read:
    /// D is traditional 401(k), AA is Roth 401(k), and W is HSA.
    /// </summary>
    public sealed class FormW2 : BaseDomainModel
    {
        public int TaxYear { get; set; }
        public string Employer { get; set; } = "";
        public Guid PersonId { get; set; }

        public decimal Wages { get; set; }
        public decimal SocialSecurityWages { get; set; }
        public decimal Traditional401k { get; set; }
        public decimal Roth401k { get; set; }
        public decimal Hsa { get; set; }
    }
}
