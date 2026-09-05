using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Model
{
    public class ExpenseSmoothingModel
    {
        public decimal ComputeSmoothedAverage(
            List<MonthlyExpenseTotal> monthlyTotals,
            ExpenseSmoothingMode mode,
            int monthsToAverage,
            decimal alpha = 0.2m,
            decimal seasonalWeight = 0.7m,
            decimal trendWeight = 0.3m)
        {
            if (!monthlyTotals.Any())
                return 0;

            // Ensure sorted oldest → newest
            monthlyTotals = monthlyTotals
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            switch (mode)
            {
                case ExpenseSmoothingMode.SimpleAverage:
                    return SimpleAverage(monthlyTotals, monthsToAverage);

                case ExpenseSmoothingMode.WeightedAverage:
                    return WeightedAverage(monthlyTotals, monthsToAverage);

                case ExpenseSmoothingMode.Exponential:
                    return ExponentialSmoothing(monthlyTotals, alpha);

                case ExpenseSmoothingMode.Seasonal:
                    return SeasonalAverage(monthlyTotals);

                case ExpenseSmoothingMode.Hybrid:
                    return Hybrid(monthlyTotals, monthsToAverage, seasonalWeight, trendWeight);

                default:
                    return SimpleAverage(monthlyTotals, monthsToAverage);
            }
        }

        // ------------------------------------------------------------
        // 1. Simple Moving Average (SMA)
        // ------------------------------------------------------------
        private decimal SimpleAverage(List<MonthlyExpenseTotal> totals, int months)
        {
            var slice = totals.TakeLast(months).ToList();
            return slice.Average(t => t.Amount);
        }

        // ------------------------------------------------------------
        // 2. Weighted Moving Average (WMA)
        // ------------------------------------------------------------
        private decimal WeightedAverage(List<MonthlyExpenseTotal> totals, int months)
        {
            var slice = totals.TakeLast(months).ToList();
            int n = slice.Count;

            // More weight for recent months
            var weights = Enumerable.Range(1, n).Select(i => (decimal)i).ToList();
            decimal weightSum = weights.Sum();

            decimal weightedTotal = 0;
            for (int i = 0; i < n; i++)
                weightedTotal += slice[i].Amount * weights[i];

            return weightedTotal / weightSum;
        }

        // ------------------------------------------------------------
        // 3. Exponential Smoothing (ES)
        // ------------------------------------------------------------
        private decimal ExponentialSmoothing(List<MonthlyExpenseTotal> totals, decimal alpha)
        {
            decimal smoothed = totals.First().Amount;

            foreach (var t in totals.Skip(1))
            {
                smoothed = alpha * t.Amount + (1 - alpha) * smoothed;
            }

            return smoothed;
        }

        // ------------------------------------------------------------
        // 4. Seasonal Decomposition (SD)
        // ------------------------------------------------------------
        private decimal SeasonalAverage(List<MonthlyExpenseTotal> totals)
        {
            int nextMonth = DateTime.Today.AddMonths(1).Month;

            var seasonalValues = totals
                .Where(t => t.Month == nextMonth)
                .Select(t => t.Amount)
                .ToList();

            return seasonalValues.Any() ? seasonalValues.Average() : totals.Last().Amount;
        }

        // ------------------------------------------------------------
        // 5. Hybrid Seasonal + Trend (HST)
        // ------------------------------------------------------------
        private decimal Hybrid(
            List<MonthlyExpenseTotal> totals,
            int monthsToAverage,
            decimal seasonalWeight,
            decimal trendWeight)
        {
            var seasonal = SeasonalAverage(totals);
            var trend = WeightedAverage(totals, monthsToAverage);

            return (seasonalWeight * seasonal) + (trendWeight * trend);
        }
    }
}
