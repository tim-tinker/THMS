using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Planning
{
    public sealed class PromotionPaymentPlan
    {
        public Guid PromotionId { get; init; }
        public string Promotion { get; init; } = "";
        public PromoType Type { get; init; }
        public DateTime AsOf { get; init; }
        public DateTime Deadline { get; init; }
        public RecurrenceFrequency Frequency { get; init; }
        public bool FrequencyEditable { get; init; }
        public decimal RequiredAmount { get; init; }
        public decimal CurrentBalance { get; init; }
    }

    public static class PromotionPaymentPlanner
    {
        public static readonly RecurrenceFrequency[] LumpSumFrequencies =
        [
            RecurrenceFrequency.Weekly,
            RecurrenceFrequency.BiWeekly,
            RecurrenceFrequency.Monthly
        ];

        public static decimal? StatementBalanceOf(AccountStatement? statement) => statement switch
        {
            BankStatement bank => bank.StatementBalance,
            LoanStatement loan => loan.StatementBalance,
            MortgageStatement mortgage => mortgage.StatementBalance,
            CreditCardStatement card => card.StatementBalance,
            _ => null
        };

        public static decimal NonPromotionBalance(decimal statementBalance, IEnumerable<PromotionalBalance> promotions)
        {
            ArgumentNullException.ThrowIfNull(promotions);
            return statementBalance - promotions.Sum(promo => promo.CurrentBalance);
        }

        public static string FrequencyName(RecurrenceFrequency frequency) => frequency switch
        {
            RecurrenceFrequency.Weekly => "Weekly",
            RecurrenceFrequency.BiWeekly => "Biweekly",
            _ => "Monthly"
        };

        public static int RemainingPeriods(DateTime asOf, DateTime deadline, RecurrenceFrequency frequency)
        {
            asOf = asOf.Date;
            deadline = deadline.Date;
            if (deadline <= asOf)
                return 1;

            var days = (deadline - asOf).TotalDays;
            if (frequency == RecurrenceFrequency.Weekly)
                return Math.Max(1, (int)Math.Round(days / 7d, MidpointRounding.AwayFromZero));
            if (frequency == RecurrenceFrequency.BiWeekly)
                return Math.Max(1, (int)Math.Round(days / 14d, MidpointRounding.AwayFromZero));

            var months = (deadline.Year - asOf.Year) * 12 + (deadline.Month - asOf.Month);
            if (deadline.Day < asOf.Day)
                months--;
            return Math.Max(1, months);
        }

        public static decimal RequiredThisPayment(
            PromoType type,
            decimal balance,
            DateTime asOf,
            DateTime deadline,
            RecurrenceFrequency frequency)
        {
            balance = Math.Max(0m, balance);
            if (balance == 0)
                return 0;

            var effective = type == PromoType.EqualPayments
                ? RecurrenceFrequency.Monthly
                : frequency;
            var periods = RemainingPeriods(asOf, deadline, effective);
            return MoneyCeiling(balance / periods);
        }

        public static decimal RequiredThisPayment(PromotionalBalance promo, DateTime asOf, RecurrenceFrequency? frequency = null)
        {
            ArgumentNullException.ThrowIfNull(promo);
            return RequiredThisPayment(
                promo.Type,
                promo.CurrentBalance,
                asOf,
                promo.Deadline,
                frequency ?? RecurrenceFrequency.Monthly);
        }

        public static List<PromotionPaymentPlan> Plan(IEnumerable<PromotionalBalance> promotions, DateTime asOf)
        {
            ArgumentNullException.ThrowIfNull(promotions);
            asOf = asOf.Date;
            return promotions
                .Select(promo =>
                {
                    var frequency = RecurrenceFrequency.Monthly;
                    return new PromotionPaymentPlan
                    {
                        PromotionId = promo.Id,
                        Promotion = Describe(promo),
                        Type = promo.Type,
                        AsOf = asOf,
                        Deadline = promo.Deadline,
                        Frequency = frequency,
                        FrequencyEditable = promo.Type == PromoType.LumpSum,
                        RequiredAmount = RequiredThisPayment(promo, asOf, frequency),
                        CurrentBalance = Math.Max(0m, promo.CurrentBalance)
                    };
                })
                .ToList();
        }

        private static string Describe(PromotionalBalance promo)
        {
            var type = PromoTypeDisplay.Name(promo.Type);
            var due = promo.Deadline.Year > 1 ? $" due {promo.Deadline:d}" : "";
            return $"{type} {promo.CurrentBalance:c2}{due}";
        }

        private static decimal MoneyCeiling(decimal value) =>
            Math.Ceiling(value * 100m) / 100m;
    }
}
