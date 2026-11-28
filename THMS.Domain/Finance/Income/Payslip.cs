namespace THMS.Domain.Finance.Income
{
    /// <summary>
    /// One paycheck stub. Year-to-date amounts include this paycheck.
    /// </summary>
    public sealed class Payslip : BaseDomainModel
    {
        public DateTime PayDate { get; set; }
        public string Employer { get; set; } = "";
        public Guid PersonId { get; set; }

        public decimal Gross { get; set; }
        public decimal GrossYearToDate { get; set; }

        public decimal Traditional401k { get; set; }
        public decimal Traditional401kYearToDate { get; set; }

        public decimal Roth401k { get; set; }
        public decimal Roth401kYearToDate { get; set; }

        public decimal SocialSecurityTax { get; set; }
        public decimal SocialSecurityTaxYearToDate { get; set; }

        public decimal MedicareTax { get; set; }
        public decimal MedicareTaxYearToDate { get; set; }

        public decimal FederalIncomeTax { get; set; }
        public decimal FederalIncomeTaxYearToDate { get; set; }

        /// <summary>
        /// Taxable cost of employer group-term life insurance. It is imputed income, not cash pay.
        /// </summary>
        public decimal GroupTermLifeTaxable { get; set; }
        public decimal GroupTermLifeTaxableYearToDate { get; set; }

        public decimal Hsa { get; set; }
        public decimal HsaYearToDate { get; set; }
    }
}
