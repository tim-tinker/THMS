using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Budget
{
    public static class BudgetPeriodCalculator
    {
        public static (DateTime Start, DateTime End) PeriodContaining(DateTime date, BudgetFrequency frequency)
        {
            date = date.Date;
            return frequency switch
            {
                BudgetFrequency.Weekly => WeekContaining(date),
                BudgetFrequency.Biweekly => BiweekContaining(date),
                BudgetFrequency.Monthly => MonthContaining(date),
                BudgetFrequency.Quarterly => QuarterContaining(date),
                BudgetFrequency.Annual => (
                    new DateTime(date.Year, 1, 1),
                    new DateTime(date.Year, 12, 31)),
                _ => MonthContaining(date)
            };
        }

        public static (DateTime Start, DateTime End) NextPeriod(DateTime previousEnd, BudgetFrequency frequency) =>
            PeriodContaining(previousEnd.Date.AddDays(1), frequency);

        public static IEnumerable<(DateTime Start, DateTime End)> PeriodsOverlapping(
            DateTime from,
            DateTime to,
            BudgetFrequency frequency)
        {
            var (start, end) = PeriodContaining(from.Date, frequency);
            var last = to.Date;
            while (start <= last)
            {
                yield return (start, end);
                (start, end) = NextPeriod(end, frequency);
            }
        }

        private static (DateTime Start, DateTime End) MonthContaining(DateTime date)
        {
            var start = new DateTime(date.Year, date.Month, 1);
            return (start, start.AddMonths(1).AddDays(-1));
        }

        private static (DateTime Start, DateTime End) QuarterContaining(DateTime date)
        {
            var quarter = (date.Month - 1) / 3;
            var start = new DateTime(date.Year, quarter * 3 + 1, 1);
            return (start, start.AddMonths(3).AddDays(-1));
        }

        private static (DateTime Start, DateTime End) WeekContaining(DateTime date)
        {
            var start = StartOfWeek(date);
            return (start, start.AddDays(6));
        }

        private static (DateTime Start, DateTime End) BiweekContaining(DateTime date)
        {
            var epoch = StartOfWeek(new DateTime(2024, 1, 1));
            var days = (date.Date - epoch).TotalDays;
            var block = (int)Math.Floor(days / 14d);
            var start = epoch.AddDays(block * 14);
            return (start, start.AddDays(13));
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            var diff = ((int)date.DayOfWeek + 6) % 7;
            return date.Date.AddDays(-diff);
        }
    }
}
