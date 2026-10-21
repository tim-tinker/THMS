using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Budget
{
    public static class BudgetRecommendation
    {
        public static decimal Compute(
            IEnumerable<(DateTime Start, DateTime End, decimal Actual)> periods,
            BudgetFrequency frequency,
            DateTime? cycleStart = null,
            DateTime? asOf = null)
        {
            var today = (asOf ?? DateTime.Today).Date;
            var closed = periods
                .Where(period => period.End.Date < today)
                .OrderBy(period => period.Start)
                .ToList();
            if (closed.Count == 0)
                return 0;

            var (levelWindow, seasonalWindow) = Windows(frequency);
            var levelPeriods = closed.TakeLast(levelWindow).ToList();
            if (seasonalWindow == 0)
                return NonNegative(WeightedAverage(Amounts(levelPeriods)));

            var indexes = SeasonalIndexes(
                closed.TakeLast(seasonalWindow).ToList(),
                frequency,
                cycleStart);
            var deseasonalized = levelPeriods
                .Select(period => Deseasonalize(
                    period.Actual,
                    IndexFor(indexes, BudgetPeriodCalculator.PeriodSlot(period.Start, frequency, cycleStart))))
                .ToList();
            var target = BudgetPeriodCalculator.PeriodSlot(today, frequency, cycleStart);
            return NonNegative(WeightedAverage(deseasonalized) * IndexFor(indexes, target));
        }

        private static (int Level, int Seasonal) Windows(BudgetFrequency frequency) => frequency switch
        {
            BudgetFrequency.Weekly => (13, 104),
            BudgetFrequency.Biweekly => (13, 52),
            BudgetFrequency.Quarterly => (4, 12),
            BudgetFrequency.Annual => (5, 0),
            _ => (12, 36)
        };

        private static Dictionary<int, decimal> SeasonalIndexes(
            List<(DateTime Start, DateTime End, decimal Actual)> periods,
            BudgetFrequency frequency,
            DateTime? cycleStart)
        {
            var estimated = periods
                .Select(period => (
                    Slot: BudgetPeriodCalculator.PeriodSlot(period.Start, frequency, cycleStart),
                    period.Start,
                    period.Actual))
                .GroupBy(period => period.Slot)
                .Where(group => group.Count() >= 2)
                .Select(group => (
                    Slot: group.Key,
                    Average: WeightedAverage(group.OrderBy(period => period.Start).Select(period => period.Actual).ToList())))
                .ToList();
            if (estimated.Count == 0)
                return new Dictionary<int, decimal>();

            var mean = estimated.Average(slot => slot.Average);
            if (mean <= 0)
                return new Dictionary<int, decimal>();

            return estimated.ToDictionary(slot => slot.Slot, slot => slot.Average / mean);
        }

        private static decimal IndexFor(Dictionary<int, decimal> indexes, int slot) =>
            indexes.TryGetValue(slot, out var index) ? index : 1m;

        private static decimal Deseasonalize(decimal amount, decimal index) =>
            index > 0 ? amount / index : amount;

        private static List<decimal> Amounts(List<(DateTime Start, DateTime End, decimal Actual)> periods) =>
            periods.Select(period => period.Actual).ToList();

        private static decimal WeightedAverage(IReadOnlyList<decimal> oldestToNewest)
        {
            var count = oldestToNewest.Count;
            if (count == 0)
                return 0;

            decimal weighted = 0;
            decimal weightSum = 0;
            for (var i = 0; i < count; i++)
            {
                var weight = i + 1m;
                weighted += oldestToNewest[i] * weight;
                weightSum += weight;
            }

            return weighted / weightSum;
        }

        private static decimal NonNegative(decimal amount) =>
            amount < 0 ? 0 : amount;
    }
}
