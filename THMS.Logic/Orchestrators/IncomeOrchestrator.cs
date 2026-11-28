using THMS.Data.Stores;
using THMS.Domain.Finance.Income;
using THMS.Logic.ViewModels.Income;

namespace THMS.Logic.Orchestrators
{
    public class IncomeOrchestrator
    {
        private readonly IIncomeDataStore _income;

        public IncomeOrchestrator()
            : this(new DataStoreFactory().GetIncomeStore())
        {
        }

        public IncomeOrchestrator(IIncomeDataStore income)
        {
            _income = income;
        }

        public IReadOnlyList<PayslipListRow> GetPayslipRows() =>
            _income.GetAll()
                .Select(payslip => new PayslipListRow
                {
                    Id = payslip.Id,
                    PayDate = payslip.PayDate,
                    Gross = payslip.Gross,
                    GrossYearToDate = payslip.GrossYearToDate,
                    Traditional401kYearToDate = payslip.Traditional401kYearToDate,
                    Roth401kYearToDate = payslip.Roth401kYearToDate,
                    HsaYearToDate = payslip.HsaYearToDate
                })
                .ToList();

        public IReadOnlyList<Payslip> GetPayslips() => _income.GetAll();

        public Payslip? GetPayslip(Guid id) => _income.Get(id);

        public void Save(Payslip payslip)
        {
            ArgumentNullException.ThrowIfNull(payslip);
            payslip.PayDate = payslip.PayDate.Date;
            payslip.Employer = payslip.Employer.Trim();
            if (payslip.Employer.Length == 0)
                throw new InvalidOperationException("Employer is required.");
            if (payslip.PersonId == Guid.Empty)
                throw new InvalidOperationException("Person is required.");
            RequireNonNegative(payslip.Gross, "Gross");
            RequireNonNegative(payslip.Traditional401k, "Traditional 401(k)");
            RequireNonNegative(payslip.Roth401k, "Roth 401(k)");
            RequireNonNegative(payslip.SocialSecurityTax, "Social Security tax");
            RequireNonNegative(payslip.MedicareTax, "Medicare tax");
            RequireNonNegative(payslip.FederalIncomeTax, "Federal income tax");
            RequireNonNegative(payslip.GroupTermLifeTaxable, "GTLI taxable");
            RequireNonNegative(payslip.Hsa, "HSA");
            RequireCoversCurrent(payslip.GrossYearToDate, payslip.Gross, "gross");
            RequireCoversCurrent(payslip.Traditional401kYearToDate, payslip.Traditional401k, "traditional 401(k)");
            RequireCoversCurrent(payslip.Roth401kYearToDate, payslip.Roth401k, "Roth 401(k)");
            RequireCoversCurrent(payslip.SocialSecurityTaxYearToDate, payslip.SocialSecurityTax, "Social Security tax");
            RequireCoversCurrent(payslip.MedicareTaxYearToDate, payslip.MedicareTax, "Medicare tax");
            RequireCoversCurrent(payslip.FederalIncomeTaxYearToDate, payslip.FederalIncomeTax, "federal income tax");
            RequireCoversCurrent(payslip.GroupTermLifeTaxableYearToDate, payslip.GroupTermLifeTaxable, "GTLI taxable");
            RequireCoversCurrent(payslip.HsaYearToDate, payslip.Hsa, "HSA");
            _income.Save(payslip);
        }

        public void Delete(Guid id) => _income.Delete(id);

        public IReadOnlyList<FormW2ListRow> GetFormW2Rows() =>
            _income.GetFormW2s()
                .Select(form => new FormW2ListRow
                {
                    Id = form.Id,
                    TaxYear = form.TaxYear,
                    Employer = form.Employer,
                    Wages = form.Wages,
                    SocialSecurityWages = form.SocialSecurityWages,
                    Traditional401k = form.Traditional401k,
                    Roth401k = form.Roth401k,
                    Hsa = form.Hsa
                })
                .ToList();

        public IReadOnlyList<FormW2> GetFormW2s() => _income.GetFormW2s();

        public FormW2? GetFormW2(Guid id) => _income.GetFormW2(id);

        public void Save(FormW2 form)
        {
            ArgumentNullException.ThrowIfNull(form);
            form.Employer = form.Employer.Trim();
            if (form.Employer.Length == 0)
                throw new InvalidOperationException("Employer is required.");
            if (form.PersonId == Guid.Empty)
                throw new InvalidOperationException("Person is required.");
            if (form.TaxYear < 1950 || form.TaxYear > 2100)
                throw new InvalidOperationException("Tax year must be from 1950 through 2100.");
            RequireNonNegative(form.Wages, "Wages");
            RequireNonNegative(form.SocialSecurityWages, "Social Security wages");
            RequireNonNegative(form.Traditional401k, "Traditional 401(k)");
            RequireNonNegative(form.Roth401k, "Roth 401(k)");
            RequireNonNegative(form.Hsa, "HSA");
            _income.Save(form);
        }

        public void DeleteFormW2(Guid id) => _income.DeleteFormW2(id);

        private static void RequireNonNegative(decimal amount, string name)
        {
            if (amount < 0m)
                throw new InvalidOperationException($"{name} cannot be negative.");
        }

        private static void RequireCoversCurrent(decimal yearToDate, decimal current, string name)
        {
            if (yearToDate < 0m)
                throw new InvalidOperationException($"Year-to-date {name} cannot be negative.");
            if (yearToDate < current)
                throw new InvalidOperationException($"Year-to-date {name} cannot be less than this paycheck's {name}.");
        }
    }
}
