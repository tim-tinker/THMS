using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;

namespace THMS.Logic.Orchestrators.Finance
{
    public class AccountOrchestrator
    {
        private readonly DataStoreFactory _storeFactory = new();
        private readonly IAccountDataStore _accountStore;

        public AccountOrchestrator()
        {
            _accountStore = _storeFactory.GetAccountStore();
        }

        // ------------------------------------------------------------
        // Create or update an account
        // ------------------------------------------------------------
        public void Save(Account account)
        {
            Validate(account);
            Normalize(account);

            _accountStore.UpsertAccount(account);
        }

        public void Delete(Guid accountId)
        {
            _accountStore.DeleteAccount(accountId);
        }

        // ------------------------------------------------------------
        // Update posted balance + BalanceAsOf after transaction import
        // ------------------------------------------------------------
        public void UpdatePostedBalance(Guid accountId, decimal newPostedBalance, DateTime asOfDate)
        {
            var account = _accountStore.GetAccount(accountId);
            if (account == null)
                throw new InvalidOperationException($"Account {accountId} not found.");

            switch (account)
            {
                case BankAccount bank:
                    bank.PostedBalance = newPostedBalance;
                    break;

                case CreditAccount credit:
                    credit.PostedBalance = newPostedBalance;
                    break;

                case InvestmentAccount invest:
                    invest.CashBalance = newPostedBalance;
                    break;

                case LoanAccount loan:
                    loan.Principal = newPostedBalance;
                    break;

                case MortgageAccount mortgage:
                    mortgage.Principal = newPostedBalance;
                    break;

                case InternalAccount internalAcc:
                    // Internal accounts compute balance from transactions,
                    // so no direct posted balance update.
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported account type: {account.GetType().Name}");
            }

            account.BalanceAsOf = asOfDate;
            _accountStore.UpsertAccount(account);
        }

        // ------------------------------------------------------------
        // Retrieve helpers
        // ------------------------------------------------------------
        public Account? GetAccount(Guid id) =>
            _accountStore.GetAccount(id);

        public IEnumerable<Account> GetAllAccounts() =>
            _accountStore.GetAllAccounts();

        // ------------------------------------------------------------
        // Validation
        // ------------------------------------------------------------
        private void Validate(Account account)
        {
            if (string.IsNullOrWhiteSpace(account.Name))
                throw new ArgumentException("Account name is required.");

            if (string.IsNullOrWhiteSpace(account.Institution))
                throw new ArgumentException("Institution is required.");

            if (string.IsNullOrWhiteSpace(account.AccountNumber))
                throw new ArgumentException("Account number is required.");

            if (account.Type == AccountType.CreditCard ||
                account.Type == AccountType.LineOfCredit)
            {
                if (account is CreditAccount credit)
                {
                    if (credit.CreditLimit <= 0)
                        throw new ArgumentException("Credit limit must be positive.");
                }
            }
        }

        // ------------------------------------------------------------
        // Normalization
        // ------------------------------------------------------------
        private void Normalize(Account account)
        {
            // Remove spaces, dashes, etc.
            account.AccountNumber = new string(
                account.AccountNumber.Where(char.IsDigit).ToArray()
            );
        }
    }
}
