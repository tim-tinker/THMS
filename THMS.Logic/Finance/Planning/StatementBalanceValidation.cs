using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Model;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.Planning
{
    public static class StatementBalanceValidation
    {
        public static StatementValidation Build(
            Account account,
            AccountStatement statement,
            AccountStatement previous,
            IEnumerable<UnifiedTransactionView> transactions)
        {
            ArgumentNullException.ThrowIfNull(account);
            ArgumentNullException.ThrowIfNull(statement);
            ArgumentNullException.ThrowIfNull(previous);
            ArgumentNullException.ThrowIfNull(transactions);

            var opening = BalanceOf(previous);
            var balance = opening;
            var rows = new List<StatementValidationRow>();
            foreach (var item in DisplayAmounts(account, transactions))
            {
                balance += item.Amount;
                rows.Add(new StatementValidationRow
                {
                    Date = item.Date,
                    Description = item.Description,
                    Amount = item.Amount,
                    Balance = balance
                });
            }

            return new StatementValidation
            {
                PreviousStatementDate = previous.StatementDate.Date,
                OpeningBalance = opening,
                StatementDate = statement.StatementDate.Date,
                StatementBalance = BalanceOf(statement),
                Rows = rows,
                EndingBalance = balance
            };
        }

        public static decimal BalanceOf(AccountStatement statement) => statement switch
        {
            BankStatement bank => bank.StatementBalance,
            InvestmentStatement investment => investment.StatementBalance,
            LoanStatement loan => loan.StatementBalance,
            MortgageStatement mortgage => mortgage.StatementBalance,
            CreditCardStatement card => card.StatementBalance,
            UtilityStatement utility => utility.StatementBalance,
            _ => 0m
        };

        private static IEnumerable<(DateTime Date, string Description, decimal Amount)> DisplayAmounts(
            Account account,
            IEnumerable<UnifiedTransactionView> transactions) =>
            transactions
                .Select(view => (
                    view.Date.Date,
                    Description: string.IsNullOrWhiteSpace(view.Description) ? "" : view.Description.Trim(),
                    Amount: PostedBalanceCalculator.ToDisplayBalance(account, view.Amount),
                    view.Id))
                .OrderBy(item => item.Date)
                .ThenByDescending(item => item.Amount)
                .ThenBy(item => item.Id)
                .Select(item => (item.Date, item.Description, item.Amount));
    }
}
