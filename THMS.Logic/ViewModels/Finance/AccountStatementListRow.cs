using System.Globalization;
using THMS.Domain.Finance.Planning;

namespace THMS.Logic.ViewModels.Finance
{
    public sealed record AccountStatementListRow
    {
        public const string NotApplicable = "N/A";

        public Guid Id { get; init; }
        public string Type { get; init; } = "";
        public string StatementDate { get; init; } = NotApplicable;
        public string DueDate { get; init; } = NotApplicable;
        public string AmountDue { get; init; } = NotApplicable;
        public string Interest { get; init; } = NotApplicable;
        public string StatementBalance { get; init; } = NotApplicable;
        public string EscrowBalance { get; init; } = NotApplicable;
        public string Promotions { get; init; } = NotApplicable;
        public string Usage { get; init; } = NotApplicable;
        public string Charges { get; init; } = NotApplicable;
        public string Notes { get; init; } = "";

        public static AccountStatementListRow From(AccountStatement statement, decimal? periodInterest = null)
        {
            ArgumentNullException.ThrowIfNull(statement);

            return statement switch
            {
                BankStatement bank => new AccountStatementListRow
                {
                    Id = bank.Id,
                    Type = DisplayType(bank.Type),
                    StatementDate = Date(bank.StatementDate),
                    Notes = bank.Notes ?? "",
                    Interest = InterestCell(periodInterest),
                    StatementBalance = Money(bank.StatementBalance)
                },
                LoanStatement loan => ObligationRow(loan) with
                {
                    StatementBalance = Money(loan.StatementBalance),
                    Interest = InterestCell(periodInterest)
                },
                MortgageStatement mortgage => ObligationRow(mortgage) with
                {
                    StatementBalance = Money(mortgage.StatementBalance),
                    EscrowBalance = Money(mortgage.EscrowBalance),
                    Interest = InterestCell(periodInterest)
                },
                CreditCardStatement card => ObligationRow(card) with
                {
                    StatementBalance = Money(card.StatementBalance),
                    Interest = InterestCell(periodInterest),
                    Promotions = FormatPromotions(card.Promotions)
                },
                UtilityStatement utility => ObligationRow(utility) with
                {
                    Usage = FormatUsage(utility.Usage),
                    Charges = FormatCharges(utility.Charges.Select(c => (c.Description, c.Amount)))
                },
                ServiceStatement service => ObligationRow(service) with
                {
                    Charges = FormatCharges(service.Charges.Select(c => (c.Description, c.Amount)))
                },
                _ => ObligationRow(statement)
            };

            static AccountStatementListRow ObligationRow(AccountStatement source) => new()
            {
                Id = source.Id,
                Type = DisplayType(source.Type),
                StatementDate = Date(source.StatementDate),
                DueDate = Date(source.DueDate),
                AmountDue = Money(source.AmountDue),
                Notes = source.Notes ?? ""
            };
        }

        public static string DisplayType(StatementType type) => type switch
        {
            StatementType.CreditCard => "Credit Card",
            StatementType.Bank => "Bank",
            StatementType.Loan => "Loan",
            StatementType.Mortgage => "Mortgage",
            StatementType.Utility => "Utility",
            StatementType.Service => "Service",
            StatementType.Insurance => "Insurance",
            _ => type.ToString()
        };

        private static string Date(DateTime value) => value.ToString("d");
        private static string Money(decimal value) => value.ToString("c2");
        private static string InterestCell(decimal? periodInterest) =>
            periodInterest is decimal value ? Money(value) : NotApplicable;

        private static string FormatPromotions(IEnumerable<PromotionalBalance> promotions)
        {
            var items = promotions
                .Select(p => $"{DisplayPromo(p.Type)} {Money(p.Amount)} by {Date(p.Deadline)}")
                .ToList();
            return items.Count == 0 ? "" : string.Join("; ", items);
        }

        private static string FormatUsage(IEnumerable<UtilityUsageRecord> usage)
        {
            var items = usage
                .Select(u => $"{u.Type} {u.Amount.ToString("N2", CultureInfo.CurrentCulture)} @ {u.Rate.ToString("0.####", CultureInfo.CurrentCulture)}")
                .ToList();
            return items.Count == 0 ? "" : string.Join("; ", items);
        }

        private static string FormatCharges(IEnumerable<(string Description, decimal Amount)> charges)
        {
            var items = charges
                .Select(c => string.IsNullOrWhiteSpace(c.Description)
                    ? Money(c.Amount)
                    : $"{c.Description} {Money(c.Amount)}")
                .ToList();
            return items.Count == 0 ? "" : string.Join("; ", items);
        }

        private static string DisplayPromo(PromoType type) => type switch
        {
            PromoType.LumpSum => "Lump sum",
            PromoType.EqualPayments => "Equal payments",
            _ => type.ToString()
        };
    }
}
