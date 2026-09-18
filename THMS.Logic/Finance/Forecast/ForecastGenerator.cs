using THMS.Domain.Finance.Transactions;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.Forecast
{
    public class ForecastGenerator
    {
        private const int MaxOccurrencesPerRule = 500;

        public List<UnifiedTransactionView> GenerateForecast(
            Guid accountId,
            DateTime from,
            DateTime to,
            IEnumerable<RecurringSingleTransactionRule> singleRules,
            IEnumerable<RecurringTransferRule> transferRules)
        {
            var results = new List<UnifiedTransactionView>();

            foreach (var rule in singleRules.Where(r => r.IsActive))
            {
                if (rule.AccountId == accountId)
                    results.AddRange(ExpandSingleRule(rule, from, to));
                else
                    results.AddRange(ExpandIncomingTransferSplits(rule, accountId, from, to));
            }

            foreach (var rule in transferRules.Where(r => r.IsActive &&
                         (r.FromAccountId == accountId || r.ToAccountId == accountId)))
                results.AddRange(ExpandTransferRule(rule, accountId, from, to));

            return UnifiedTransactionView.OrderForRunningBalance(results).ToList();
        }

        private static IEnumerable<UnifiedTransactionView> ExpandSingleRule(
            RecurringSingleTransactionRule rule,
            DateTime from,
            DateTime to)
        {
            var next = rule.NextOccurrence;
            var count = 0;

            while (next <= to && (rule.EndDate == null || next <= rule.EndDate.Value) && count < MaxOccurrencesPerRule)
            {
                if (next >= from)
                {
                    yield return UnifiedTransactionViewBuilder.ForLedgerRow(
                        rule,
                        rule.AccountId,
                        UnifiedTransactionView.ForecastType,
                        next,
                        OccurrenceAmount(rule.Amount, rule.IsFinalPaymentDifferent, rule.EndDate, rule.FinalPaymentAmount, next),
                        Guid.NewGuid());
                }

                next = next.AddFrequency(rule.Frequency);
                count++;
            }
        }

        private static IEnumerable<UnifiedTransactionView> ExpandIncomingTransferSplits(
            RecurringSingleTransactionRule rule,
            Guid accountId,
            DateTime from,
            DateTime to)
        {
            var splits = rule.Splits
                .Where(s => SplitTransactionMath.IsTransferTo(s, accountId))
                .ToList();
            if (splits.Count == 0)
                yield break;

            var next = rule.NextOccurrence;
            var count = 0;

            while (next <= to && (rule.EndDate == null || next <= rule.EndDate.Value) && count < MaxOccurrencesPerRule)
            {
                if (next >= from)
                {
                    var amount = OccurrenceAmount(
                        rule.Amount, rule.IsFinalPaymentDifferent, rule.EndDate, rule.FinalPaymentAmount, next);
                    if (Math.Abs(amount) == Math.Abs(rule.Amount))
                    {
                        foreach (var split in splits)
                        {
                            yield return UnifiedTransactionViewBuilder.ForTransferSplit(
                                rule,
                                split,
                                accountId,
                                UnifiedTransactionView.ForecastTransferType,
                                next,
                                Guid.NewGuid());
                        }
                    }
                }

                next = next.AddFrequency(rule.Frequency);
                count++;
            }
        }

        private static IEnumerable<UnifiedTransactionView> ExpandTransferRule(
            RecurringTransferRule rule,
            Guid accountId,
            DateTime from,
            DateTime to)
        {
            var next = rule.NextOccurrence;
            var count = 0;

            while (next <= to && (rule.EndDate == null || next <= rule.EndDate.Value) && count < MaxOccurrencesPerRule)
            {
                if (next >= from)
                {
                    var amount = OccurrenceAmount(
                        rule.Amount, rule.IsFinalPaymentDifferent, rule.EndDate, rule.FinalPaymentAmount, next);
                    if (rule.FromAccountId != rule.ToAccountId && rule.ToAccountId == accountId)
                        amount = -amount;

                    yield return UnifiedTransactionViewBuilder.ForLedgerRow(
                        rule,
                        accountId,
                        UnifiedTransactionView.ForecastTransferType,
                        next,
                        amount,
                        Guid.NewGuid());
                }

                next = next.AddFrequency(rule.Frequency);
                count++;
            }
        }

        private static decimal OccurrenceAmount(
            decimal amount,
            bool isFinalPaymentDifferent,
            DateTime? endDate,
            decimal? finalPaymentAmount,
            DateTime next) =>
            isFinalPaymentDifferent && endDate.HasValue && next == endDate.Value
                ? finalPaymentAmount ?? amount
                : amount;
    }
}
