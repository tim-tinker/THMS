using THMS.Domain.Finance.Income;
using THMS.Logic.ViewModels.Income;

namespace THMS.Logic.Finance.Income
{
    /// <summary>
    /// Groups W-2s and payslips onto one tab per employer, then one row per year.
    /// A year with payslips and no W-2 keeps a placeholder. Its amounts are the latest
    /// payslip's year-to-date figures. Wages and Social Security wages stay empty,
    /// because the payslip does not report them.
    /// </summary>
    public static class IncomeEmployerTabs
    {
        public const string UnassignedCaption = "No employer";
        public const string PayslipsSoFar = "Payslips so far";
        public const string FiledW2 = "W-2";

        public static IReadOnlyList<IncomeEmployerTab> Build(
            IEnumerable<FormW2> forms,
            IEnumerable<Payslip> payslips)
        {
            ArgumentNullException.ThrowIfNull(forms);
            ArgumentNullException.ThrowIfNull(payslips);
            var formList = forms.ToList();
            var slipList = payslips.ToList();
            var display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var form in formList)
                Remember(display, form.Employer);
            foreach (var slip in slipList)
                Remember(display, slip.Employer);

            var unassigned = slipList.Where(slip => string.IsNullOrWhiteSpace(slip.Employer)).ToList();
            var home = display.Count == 1 ? display.Keys.First() : null;
            var pages = display.Keys
                .OrderBy(key => display[key], StringComparer.OrdinalIgnoreCase)
                .Select(key => Page(
                    display[key],
                    key,
                    formList.Where(form => SameEmployer(form.Employer, key)).ToList(),
                    slipList.Where(slip => SameEmployer(slip.Employer, key)
                        || (home is not null && SameEmployer(key, home) && string.IsNullOrWhiteSpace(slip.Employer))).ToList()))
                .ToList();

            if (unassigned.Count > 0 && home is null)
                pages.Add(Page(UnassignedCaption, "", [], unassigned));
            return pages;
        }

        private static void Remember(Dictionary<string, string> display, string employer)
        {
            var trimmed = employer.Trim();
            if (trimmed.Length == 0 || display.ContainsKey(trimmed))
                return;
            display[trimmed] = trimmed;
        }

        private static IncomeEmployerTab Page(
            string caption,
            string key,
            IReadOnlyList<FormW2> forms,
            IReadOnlyList<Payslip> payslips)
        {
            var years = forms.Select(form => form.TaxYear)
                .Concat(payslips.Select(slip => slip.PayDate.Year))
                .Distinct()
                .OrderByDescending(year => year)
                .Select(year => YearRow(year, forms, payslips))
                .ToList();
            return new IncomeEmployerTab
            {
                Key = key,
                Caption = caption,
                Years = years,
                PayslipIds = payslips.Select(slip => slip.Id).ToList()
            };
        }

        private static IncomeYearRow YearRow(int year, IReadOnlyList<FormW2> forms, IReadOnlyList<Payslip> payslips)
        {
            var form = forms
                .Where(item => item.TaxYear == year)
                .OrderByDescending(item => item.ModifiedOn)
                .ThenByDescending(item => item.CreatedOn)
                .FirstOrDefault();
            var latest = payslips
                .Where(slip => slip.PayDate.Year == year)
                .OrderByDescending(slip => slip.PayDate.Date)
                .ThenByDescending(slip => slip.GrossYearToDate)
                .ThenByDescending(slip => slip.ModifiedOn)
                .FirstOrDefault();
            if (form is not null)
            {
                return new IncomeYearRow
                {
                    FormId = form.Id,
                    Year = year,
                    Record = FiledW2,
                    Wages = form.Wages,
                    SocialSecurityWages = form.SocialSecurityWages,
                    Traditional401k = form.Traditional401k,
                    Roth401k = form.Roth401k,
                    Hsa = form.Hsa,
                    GrossSoFar = latest?.GrossYearToDate
                };
            }

            return new IncomeYearRow
            {
                Year = year,
                Record = PayslipsSoFar,
                Traditional401k = latest?.Traditional401kYearToDate,
                Roth401k = latest?.Roth401kYearToDate,
                Hsa = latest?.HsaYearToDate,
                GrossSoFar = latest?.GrossYearToDate
            };
        }

        private static bool SameEmployer(string left, string right) =>
            string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
