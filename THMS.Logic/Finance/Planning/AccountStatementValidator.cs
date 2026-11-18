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
                    ValidateBalanceOnly(bank.StatementBalance, findings);
                    break;
                case InvestmentStatement investment:
                    ValidateBalanceOnly(investment.StatementBalance, findings);
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
                case PropertyTaxStatement tax:
                    ValidatePropertyTax(tax, findings);
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

        private static void ValidateBalanceOnly(decimal statementBalance, List<string> findings)
        {
            if (statementBalance < 0)
                findings.Add("Statement balance cannot be negative.");
        }

        private static void ValidateMortgage(MortgageStatement mortgage, List<string> findings)
        {
            if (mortgage.EscrowBalance < 0)
                findings.Add("Mortgage escrow balance cannot be negative.");
        }

        private static void ValidateCreditCard(CreditCardStatement card, List<string> findings)
        {
            foreach (var promo in card.Promotions)
            {
                if (promo.CurrentBalance < 0)
                    findings.Add("Promotional current balance cannot be negative.");
                if (promo.InitialAmount < 0)
                    findings.Add("Promotional initial amount cannot be negative.");
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
                {
                    var name = string.IsNullOrWhiteSpace(usage.Description) ? usage.Type : usage.Description.Trim();
                    findings.Add($"Utility usage '{name}' must have a positive amount.");
                }
            }

            if (utility.Usage.Count == 0 && utility.Charges.Count == 0)
                return;

            var total = utility.Usage.Sum(usage => usage.Cost) + utility.Charges.Sum(charge => charge.Amount);
            if (!AmountsEqual(total, utility.AmountDue))
                findings.Add($"Utility usage and charges ({total:c2}) must equal amount due ({utility.AmountDue:c2}).");
        }

        private static void ValidateService(ServiceStatement service, List<string> findings)
        {
            var chargeSum = service.Charges.Sum(c => c.Amount);
            if (!AmountsEqual(chargeSum, service.AmountDue))
                findings.Add($"Service charges ({chargeSum:c2}) must equal amount due ({service.AmountDue:c2}).");
        }

        private static void ValidatePropertyTax(PropertyTaxStatement tax, List<string> findings)
        {
            if (tax.AssetId == Guid.Empty)
                findings.Add("Choose the property this statement bills.");
            if (tax.AccountId == Guid.Empty)
                findings.Add("Choose the tax collector.");
            if (tax.TaxYear < 1900 || tax.TaxYear > 2200)
                findings.Add("Enter the tax year.");
            if (!AmountsEqual(tax.LandMarketValue + tax.ImprovementMarketValue, tax.TotalMarketValue))
                findings.Add("Land market plus improvement market must equal total market.");
            if (!AmountsEqual(tax.TotalMarketValue - tax.HomesteadCap, tax.AssessedValue))
                findings.Add("Assessed value must equal total market minus the homestead cap.");

            if (tax.Jurisdictions.Count == 0)
            {
                findings.Add("Add at least one taxing jurisdiction.");
                return;
            }

            foreach (var line in tax.Jurisdictions)
            {
                var name = string.IsNullOrWhiteSpace(line.Name) ? "Jurisdiction" : line.Name.Trim();
                if (string.IsNullOrWhiteSpace(line.Name))
                    findings.Add("Each taxing jurisdiction needs a name.");
                if (line.ExemptionAmount < 0 || line.TaxableValue < 0 || line.RatePerHundred < 0 || line.TaxAmount < 0)
                    findings.Add($"{name} amounts cannot be negative.");
                var expected = decimal.Round(
                    line.TaxableValue * line.RatePerHundred / 100m,
                    2,
                    MidpointRounding.AwayFromZero);
                if (Math.Abs(expected - line.TaxAmount) > SumTolerance)
                    findings.Add($"{name}: tax {line.TaxAmount:c2} does not match taxable value times the rate ({expected:c2}).");
            }

            var total = tax.Jurisdictions.Sum(line => line.TaxAmount);
            if (!AmountsEqual(total, tax.AmountDue))
                findings.Add($"Jurisdiction taxes ({total:c2}) must equal the total due ({tax.AmountDue:c2}).");
        }

        private static bool AmountsEqual(decimal left, decimal right) =>
            Math.Abs(left - right) <= SumTolerance;
    }
}
