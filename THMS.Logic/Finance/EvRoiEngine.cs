using THMS.Domain.Energy;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Finance.Loans;
using THMS.Domain.Finance.Roi;

namespace THMS.Logic.Finance
{
    /// <summary>
    /// Computes EV ROI using monthly energy summaries, billing data, and loan amortization.
    /// </summary>
    public class EvRoiEngine
    {
        private readonly IReadOnlyCollection<MonthlyEnergySummary> _monthlySummaries;
        private readonly IReadOnlyCollection<LoanCashFlow> _loanSchedule;

        public EvRoiEngine(
            IReadOnlyCollection<MonthlyEnergySummary> monthlySummaries,
            IReadOnlyCollection<LoanCashFlow> loanSchedule)
        {
            _monthlySummaries = monthlySummaries;
            _loanSchedule = loanSchedule;
        }

        /// <summary>
        /// Computes EV ROI for all months.
        /// </summary>
        public IReadOnlyCollection<EvRoiResult> ComputeAll()
        {
            var results = new List<EvRoiResult>();

            foreach (var summary in _monthlySummaries)
            {
                var loan = _loanSchedule.FirstOrDefault(l =>
                    l.Date.Year == summary.Year &&
                    l.Date.Month == summary.Month);

                var roi = new EvRoiResult
                {
                    Year = summary.Year,
                    Month = summary.Month,

                    SolarChargeValue = summary.SolarAvoidedCost,
                    BatteryChargeValue = summary.BatteryValue,
                    GridChargeCost = summary.GridCost,
                    CommercialChargeCost = summary.CommercialChargeCost,

                    LoanPayment = loan?.PaymentAmount ?? 0,
                    LoanInterestPaid = loan?.InterestPaid ?? 0,
                    LoanPrincipalPaid = loan?.PrincipalPaid ?? 0,

                    IsPartial = summary.IsPartial || loan == null
                };

                results.Add(roi);
            }

            return results.AsReadOnly();
        }
    }
}
