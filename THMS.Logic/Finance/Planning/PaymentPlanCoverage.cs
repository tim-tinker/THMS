using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Planning
{
    public static class PaymentPlanCoverage
    {
        public static bool Covers(IEnumerable<RecurringTransferRule> rules, Guid accountId, DateTime asOf)
        {
            var day = asOf.Date;
            return rules.Any(rule =>
                rule.IsPaymentPlan
                && rule.IsActive
                && rule.ToAccountId == accountId
                && (rule.EndDate is null || rule.EndDate.Value.Date >= day));
        }
    }
}
