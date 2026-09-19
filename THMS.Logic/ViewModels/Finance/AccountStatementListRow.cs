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
        public string StatementBalance { get; init; } = NotApplicable;
        public string EscrowBalance { get; init; } = NotApplicable;
        public string Promotions { get; init; } = NotApplicable;
        public string Usage { get; init; } = NotApplicable;
        public string Charges { get; init; } = NotApplicable;
        public string Notes { get; init; } = "";

        public static AccountStatementListRow From(AccountStatement statement)
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
                    StatementBalance = Money(bank.StatementBalance)
                },
                LoanStatement loan => ObligationRow(loan) with
                {
                    StatementBalance = Money(loan.StatementBalance)
                },
                MortgageStatement mortgage => ObligationRow(mortgage) with
                {
                    StatementBalance = Money(mortgage.StatementBalance),
                    EscrowBalance = Money(mortgage.EscrowBalance)
                },
                CreditCardStatement card => ObligationRow(card) with
                {
                    StatementBalance = Money(card.StatementBalance),
                    Promotions = CountItems(card.Promotions)
                },
                UtilityStatement utility => ObligationRow(utility) with
                {
                    Usage = CountItems(utility.Usage),
                    Charges = CountItems(utility.Charges)
                },
                ServiceStatement service => ObligationRow(service) with
                {
                    Charges = CountItems(service.Charges)
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

        private static string CountItems<T>(IEnumerable<T> items)
        {
            var count = items.Count();
            return count == 0 ? "" : count.ToString(CultureInfo.CurrentCulture);
        }
    }
}
