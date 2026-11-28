using THMS.Domain.Finance.Income;
using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Finance.Planning
{
    public sealed record Retirement401kIncomeFigures
    {
        public int PlanYear { get; init; }
        public decimal? AnnualPay { get; init; }
        public decimal? PriorYearWages { get; init; }
        public decimal? CurrentPreTaxPercent { get; init; }
        public decimal? CurrentRothPercent { get; init; }
        public decimal? PeriodGross { get; init; }
        public string AnnualPayDetail { get; init; } = "";
        public string PriorYearWagesDetail { get; init; } = "";
        public string CurrentContributionDetail { get; init; } = "";
    }

    /// <summary>
    /// Reads annual pay and prior-year wages from payslips and W-2s.
    /// Annual pay is full-year gross: year-to-date gross plus the remaining checks
    /// in that year at the latest gross. Equity vesting left off the gross line is not included.
    /// Prior-year wages are Form W-2 box 3 from the one employer who filed that year.
    /// </summary>
    public static class Retirement401kIncome
    {
        public static Retirement401kIncomeFigures Estimate(
            IEnumerable<Payslip> payslips,
            IEnumerable<FormW2> forms,
            PayFrequency frequency,
            int planYear)
        {
            ArgumentNullException.ThrowIfNull(payslips);
            ArgumentNullException.ThrowIfNull(forms);

            var slips = payslips.ToList();
            var inYear = Latest(slips.Where(payslip => payslip.PayDate.Year == planYear));
            var latest = inYear ?? Latest(slips);
            var annual = AnnualPay(inYear, latest, planYear, frequency);
            var prior = PriorYearWages(forms, planYear - 1);
            var current = CurrentContribution(latest);
            return new Retirement401kIncomeFigures
            {
                PlanYear = planYear,
                AnnualPay = annual.Amount,
                PriorYearWages = prior.Amount,
                CurrentPreTaxPercent = current.PreTax,
                CurrentRothPercent = current.Roth,
                PeriodGross = current.PeriodGross,
                AnnualPayDetail = annual.Detail,
                PriorYearWagesDetail = prior.Detail,
                CurrentContributionDetail = current.Detail
            };
        }

        private static Payslip? Latest(IEnumerable<Payslip> payslips) =>
            payslips
                .OrderByDescending(payslip => payslip.PayDate.Date)
                .ThenByDescending(payslip => payslip.GrossYearToDate)
                .ThenByDescending(payslip => payslip.ModifiedOn)
                .FirstOrDefault();

        private static (decimal? Amount, string Detail) AnnualPay(
            Payslip? inYear,
            Payslip? latest,
            int planYear,
            PayFrequency frequency)
        {
            if (inYear is not null)
            {
                var remaining = RemainingPeriods(inYear.PayDate, frequency);
                var annual = Money(inYear.GrossYearToDate + remaining * inYear.Gross);
                var through = $"{planYear} gross pay through {inYear.PayDate:d}";
                if (remaining == 0)
                    return (annual, $"{through}.");

                var checks = remaining == 1 ? "check" : "checks";
                var period = Retirement401kCalculator.PeriodName(frequency);
                return (annual, $"{through}, plus {remaining} {period} {checks} at {inYear.Gross:c2}.");
            }

            if (latest is null || latest.Gross <= 0m)
                return (null, $"No {planYear} payslips yet. Enter annual pay.");

            var periods = Retirement401kCalculator.PeriodsPerYear(frequency);
            var projected = Money(latest.Gross * periods);
            var name = Retirement401kCalculator.PeriodName(frequency);
            return (projected,
                $"No {planYear} payslips yet. Annual pay is the {latest.PayDate:d} gross of {latest.Gross:c2} times {periods} {name} checks.");
        }

        private static (decimal? PreTax, decimal? Roth, decimal? PeriodGross, string Detail) CurrentContribution(Payslip? latest)
        {
            if (latest is null || latest.Gross <= 0m)
                return (null, null, null, "No payslip yet. Current contribution percentages are unknown.");

            var preTax = Percent(latest.Traditional401k, latest.Gross);
            var roth = Percent(latest.Roth401k, latest.Gross);
            return (
                preTax,
                roth,
                latest.Gross,
                $"Latest paycheck {latest.PayDate:d} is {preTax:0.00}% pre-tax and {roth:0.00}% Roth. Catch-up is not listed separately on the payslip.");
        }

        private static decimal Percent(decimal amount, decimal gross) =>
            decimal.Round(amount / gross * 100m, 2, MidpointRounding.AwayFromZero);

        private static (decimal? Amount, string Detail) PriorYearWages(IEnumerable<FormW2> forms, int priorYear)
        {
            var matches = forms.Where(form => form.TaxYear == priorYear).ToList();
            if (matches.Count == 0)
                return (null, $"No {priorYear} W-2 yet. Enter box 3 from the employer that sponsors this plan.");

            var employers = matches
                .Select(form => form.Employer.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (employers.Count != 1)
                return (null, $"More than one employer has a {priorYear} W-2. Enter box 3 from the employer that sponsors this plan.");

            var form = matches
                .OrderByDescending(item => item.ModifiedOn)
                .ThenByDescending(item => item.CreatedOn)
                .First();
            var name = string.IsNullOrWhiteSpace(form.Employer) ? "the plan sponsor" : form.Employer.Trim();
            return (Money(form.SocialSecurityWages), $"{priorYear} Form W-2, box 3, {name}.");
        }

        private static int RemainingPeriods(DateTime payDate, PayFrequency frequency)
        {
            var start = payDate.Date;
            var remaining = 0;
            for (var step = 1; step <= 54; step++)
            {
                if (PayDateAfter(start, frequency, step).Year != start.Year)
                    break;
                remaining++;
            }

            return remaining;
        }

        private static DateTime PayDateAfter(DateTime start, PayFrequency frequency, int step) => frequency switch
        {
            PayFrequency.Weekly => start.AddDays(7 * step),
            PayFrequency.Biweekly => start.AddDays(14 * step),
            PayFrequency.Monthly => MonthLater(start, step),
            PayFrequency.Semimonthly => SemimonthlyAfter(start, step),
            _ => start.AddDays(14 * step)
        };

        private static DateTime MonthLater(DateTime start, int months)
        {
            var shifted = start.AddMonths(months);
            var day = Math.Min(start.Day, DateTime.DaysInMonth(shifted.Year, shifted.Month));
            return new DateTime(shifted.Year, shifted.Month, day);
        }

        /// <summary>Semimonthly dates are the 15th and the last day of the month.</summary>
        private static DateTime SemimonthlyAfter(DateTime start, int step)
        {
            var date = start.Date;
            for (var i = 0; i < step; i++)
                date = NextSemimonthly(date);
            return date;
        }

        private static DateTime NextSemimonthly(DateTime date)
        {
            if (date.Day < 15)
                return new DateTime(date.Year, date.Month, 15);

            var last = DateTime.DaysInMonth(date.Year, date.Month);
            if (date.Day < last)
                return new DateTime(date.Year, date.Month, last);

            var nextMonth = date.AddMonths(1);
            return new DateTime(nextMonth.Year, nextMonth.Month, 15);
        }

        private static decimal Money(decimal amount) =>
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
