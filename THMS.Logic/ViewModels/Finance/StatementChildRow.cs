using System.Globalization;
using THMS.Domain.Finance.Planning;

namespace THMS.Logic.ViewModels.Finance
{
    public sealed class StatementChildRow
    {
        public string Kind { get; init; } = "";
        public string Description { get; init; } = "";
        public string Amount { get; init; } = "";
        public string Details { get; init; } = "";

        public static List<StatementChildRow> From(AccountStatement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);

            return statement switch
            {
                CreditCardStatement card => card.Promotions
                    .Select(promo => new StatementChildRow
                    {
                        Kind = "Promotion",
                        Description = PromoTypeDisplay.Name(promo.Type),
                        Amount = Money(promo.CurrentBalance),
                        Details = PromoDetails(promo)
                    })
                    .ToList(),
                UtilityStatement utility =>
                    utility.Usage.Select(usage => new StatementChildRow
                    {
                        Kind = "Usage",
                        Description = usage.Type,
                        Amount = usage.Amount.ToString("N2", CultureInfo.CurrentCulture),
                        Details = $"Rate {usage.Rate.ToString("0.####", CultureInfo.CurrentCulture)}"
                    })
                    .Concat(utility.Charges.Select(Charge))
                    .ToList(),
                ServiceStatement service => service.Charges.Select(Charge).ToList(),
                _ => []
            };
        }

        private static string PromoDetails(PromotionalBalance promo)
        {
            var parts = new List<string>();
            if (promo.DateAcquired.Year > 1)
                parts.Add($"Acquired {promo.DateAcquired:d}");
            if (promo.InitialAmount > 0)
                parts.Add($"Initial {Money(promo.InitialAmount)}");
            if (promo.Deadline.Year > 1)
                parts.Add($"Deadline {promo.Deadline:d}");
            return string.Join("; ", parts);
        }

        private static StatementChildRow Charge(UtilityChargeLine charge) =>
            new()
            {
                Kind = "Charge",
                Description = charge.Description,
                Amount = Money(charge.Amount)
            };

        private static StatementChildRow Charge(ServiceChargeLine charge) =>
            new()
            {
                Kind = "Charge",
                Description = charge.Description,
                Amount = Money(charge.Amount)
            };

        private static string Money(decimal value) => value.ToString("c2");
    }
}
