using THMS.Domain.Energy;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Finance.Loans;
using THMS.Domain.Finance.Roi;

namespace THMS.Logic.Finance
{
    /// <summary>
    /// Computes solar ROI using monthly energy summaries, billing data, and loan amortization.
    /// </summary>
    public class SolarRoiEngine
    {
        private readonly IReadOnlyCollection<MonthlyEnergySummary> _monthlySummaries;
        private readonly IReadOnlyCollection<LoanCashFlow> _loanSchedule;
        private readonly IReadOnlyCollection<ElectricUtilityBill> _billing;

        public SolarRoiEngine(
            IReadOnlyCollection<MonthlyEnergySummary> monthlySummaries,
            IReadOnlyCollection<LoanCashFlow> loanSchedule,
            IReadOnlyCollection<ElectricUtilityBill> billingIntervals)
        {
            _monthlySummaries = monthlySummaries;
            _loanSchedule = loanSchedule;
            _billing = billingIntervals;
        }

        /// <summary>
        /// Computes solar ROI for all months.
        /// </summary>
        public IReadOnlyCollection<SolarRoiResult> ComputeAll()
        {
            var results = new List<SolarRoiResult>();

            foreach (var summary in _monthlySummaries)
            {
                var billing = _billing.FirstOrDefault(b =>
                    b.StartDate.Year == summary.Year &&
                    b.StartDate.Month == summary.Month);

                var loan = _loanSchedule.FirstOrDefault(l =>
                    l.Date.Year == summary.Year &&
                    l.Date.Month == summary.Month);

                var roi = new SolarRoiResult
                {
                    Year = summary.Year,
                    Month = summary.Month,

                    SolarAvoidedCost = summary.SolarAvoidedCost,
                    BatteryValue = summary.BatteryValue,
                    GridExportCredit = billing?.GridExportCredit ?? 0,

                    LoanPayment = loan?.PaymentAmount ?? 0,
                    LoanInterestPaid = loan?.InterestPaid ?? 0,
                    LoanPrincipalPaid = loan?.PrincipalPaid ?? 0,

                    IsPartial = billing == null || loan == null
                };

                results.Add(roi);
            }

            return results.AsReadOnly();
        }
    }
}
