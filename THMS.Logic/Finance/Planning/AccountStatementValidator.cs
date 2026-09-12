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
                case InsuranceStatement insurance:
                    ValidateInsurance(insurance, findings);
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
            if (statement.MinimumPayment < 0)
                findings.Add("Minimum payment cannot be negative.");
            if (statement.AmountDue < statement.MinimumPayment)
                findings.Add("Amount due cannot be less than the minimum payment.");
        }

        private static void ValidateBank(BankStatement bank, List<string> findings)
        {
            if (bank.PeriodStart != default && bank.PeriodStart.Date > bank.StatementDate.Date)
                findings.Add("Period start must be on or before the statement date.");
            if (bank.BeginningBalance < 0)
                findings.Add("Beginning balance cannot be negative.");
            if (bank.EndingBalance < 0)
                findings.Add("Ending balance cannot be negative.");
            if (bank.Deposits < 0)
                findings.Add("Deposits cannot be negative.");
            if (bank.Withdrawals < 0)
                findings.Add("Withdrawals cannot be negative.");
            if (bank.InterestEarned < 0)
                findings.Add("Interest earned cannot be negative.");
            if (bank.Fees < 0)
                findings.Add("Fees cannot be negative.");

            var expectedEnding = bank.ComputedEndingBalance;
            if (!AmountsEqual(expectedEnding, bank.EndingBalance))
            {
                findings.Add(
                    $"Ending balance ({bank.EndingBalance:c2}) must equal beginning + deposits + interest − withdrawals − fees ({expectedEnding:c2}).");
            }
        }

        private static void ValidateLoan(LoanStatement loan, List<string> findings)
        {
            if (loan.PrincipalBalance < 0)
                findings.Add("Loan principal balance cannot be negative.");
            if (loan.InterestBalance < 0)
                findings.Add("Loan interest balance cannot be negative.");
        }

        private static void ValidateMortgage(MortgageStatement mortgage, List<string> findings)
        {
            if (mortgage.PrincipalBalance < 0)
                findings.Add("Mortgage principal balance cannot be negative.");
            if (mortgage.InterestBalance < 0)
                findings.Add("Mortgage interest balance cannot be negative.");
            if (mortgage.EscrowBalance < 0)
                findings.Add("Mortgage escrow balance cannot be negative.");
        }

        private static void ValidateCreditCard(CreditCardStatement card, List<string> findings)
        {
            if (card.StatementBalance < 0)
                findings.Add("Credit card statement balance cannot be negative.");
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

        private static void ValidateInsurance(InsuranceStatement insurance, List<string> findings)
        {
            var total = insurance.Premium + insurance.Fees;
            if (!AmountsEqual(total, insurance.AmountDue))
                findings.Add($"Insurance premium and fees ({total:c2}) must equal amount due ({insurance.AmountDue:c2}).");
        }

        private static bool AmountsEqual(decimal left, decimal right) =>
            Math.Abs(left - right) <= SumTolerance;
    }
}
