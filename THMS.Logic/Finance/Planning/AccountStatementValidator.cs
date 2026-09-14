using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Finance.Planning
{
    public static class AccountStatementValidator
    {
        public const decimal SumTolerance = 0.01m;

        public static List<string> Validate(AccountStatement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);
            var findings = new List<string>();
            ValidateUniversal(statement, findings);
            switch (statement)
            {
                case BankStatement bank:
                    ValidateBank(bank, findings);
                    break;
                case LoanStatement loan:
                    ValidateLoan(loan, findings);
                    break;
                case MortgageStatement mortgage:
                    ValidateMortgage(mortgage, findings);
                    break;
                case CreditCardStatement card:
                    ValidateCreditCard(card, findings);
                    break;
                case UtilityStatement utility:
                    ValidateUtility(utility, findings);
                    break;
                case ServiceStatement service:
                    ValidateService(service, findings);
                    break;
            }

            return findings;
        }

        public static void EnsureValid(AccountStatement statement)
        {
            var findings = Validate(statement);
            if (findings.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, findings));
        }

        private static void ValidateUniversal(AccountStatement statement, List<string> findings)
        {
            if (statement.DueDate.Date < statement.StatementDate.Date)
                findings.Add("Due date must be on or after the statement date.");
            if (statement.AmountDue < 0)
                findings.Add("Amount due cannot be negative.");
        }

        private static void ValidateBank(BankStatement bank, List<string> findings)
        {
            if (bank.StatementBalance < 0)
                findings.Add("Statement balance cannot be negative.");
        }

        private static void ValidateLoan(LoanStatement loan, List<string> findings)
        {
            if (loan.StatementBalance < 0)
                findings.Add("Statement balance cannot be negative.");
        }

        private static void ValidateMortgage(MortgageStatement mortgage, List<string> findings)
        {
            if (mortgage.StatementBalance < 0)
                findings.Add("Statement balance cannot be negative.");
            if (mortgage.EscrowBalance < 0)
                findings.Add("Mortgage escrow balance cannot be negative.");
        }

        private static void ValidateCreditCard(CreditCardStatement card, List<string> findings)
        {
            if (card.StatementBalance < 0)
                findings.Add("Statement balance cannot be negative.");
            foreach (var promo in card.Promotions)
            {
                if (promo.Amount < 0)
                    findings.Add("Promotional balance cannot be negative.");
                if (promo.Deadline == default)
                    findings.Add("Promotional balance is missing a deadline.");
                else if (promo.Deadline.Date < card.StatementDate.Date)
                    findings.Add($"Promotional deadline {promo.Deadline:d} is before the statement date.");
            }
        }

        private static void ValidateUtility(UtilityStatement utility, List<string> findings)
        {
            foreach (var usage in utility.Usage)
            {
                if (usage.Amount <= 0)
                    findings.Add($"Utility usage '{usage.Type}' must have a positive amount.");
            }

            var chargeSum = utility.Charges.Sum(c => c.Amount);
            if (!AmountsEqual(chargeSum, utility.AmountDue))
                findings.Add($"Utility charges ({chargeSum:c2}) must equal amount due ({utility.AmountDue:c2}).");
        }

        private static void ValidateService(ServiceStatement service, List<string> findings)
        {
            var chargeSum = service.Charges.Sum(c => c.Amount);
            if (!AmountsEqual(chargeSum, service.AmountDue))
                findings.Add($"Service charges ({chargeSum:c2}) must equal amount due ({service.AmountDue:c2}).");
        }

        private static bool AmountsEqual(decimal left, decimal right) =>
            Math.Abs(left - right) <= SumTolerance;
    }
}
