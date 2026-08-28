using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.External;

namespace THMS.Logic.Orchestrators
{
    public class AccountSyncOrchestrator
    {
        private readonly ExternalFetcherFactory _externalFactory = new();
        private readonly DataStoreFactory _dataStoreFactory = new();

        private readonly IExternalAccountFetcher _accountFetcher;
        private readonly IAccountDataStore _accountStore;

        public AccountSyncOrchestrator()
        {
            _accountFetcher = _externalFactory.GetAccountFetcher();
            _accountStore = _dataStoreFactory.GetAccountStore();
        }

        public async Task<AccountSyncResult> SyncAsync(Account account)
        {
            if (account.ExternalLink is null)
                throw new InvalidOperationException("Account is not linked to Plaid.");

            var link = account.ExternalLink;

            // 1. Fetch Plaid accounts
            var plaidAccounts = await _accountFetcher.FetchAccountsAsync(link.AccessToken);

            // 2. Find matching Plaid account
            var plaid = plaidAccounts
                .FirstOrDefault(a => a.PlaidAccountId == link.PlaidAccountId);

            if (plaid is null)
                throw new InvalidOperationException(
                    $"Plaid account {link.PlaidAccountId} not found.");

            // 3. Update THMS account balances
            UpdateBalances(account, plaid);

            // 4. Update metadata (mask, subtype, etc.)
            UpdateMetadata(account, plaid);

            // 5. Persist
            _accountStore.UpsertAccount(account);

            return new AccountSyncResult
            {
                AccountId = account.Id,
                PostedBalance = account switch
                {
                    BankAccount b => b.PostedBalance,
                    CreditAccount c1 => c1.PostedBalance,
                    _ => null
                },
                CreditLimit = account is CreditAccount c2 ? c2.CreditLimit : null,
                CashBalance = account is InvestmentAccount i ? i.CashBalance : null,
                Principal = account switch
                {
                    LoanAccount l => l.Principal,
                    MortgageAccount m => m.Principal,
                    _ => null
                }
            };
        }

        private void UpdateBalances(Account account, AccountDto plaid)
        {
            switch (account)
            {
                case BankAccount bank:
                    bank.PostedBalance = plaid.Current ?? bank.PostedBalance;
                    break;

                case CreditAccount credit:
                    credit.PostedBalance = plaid.Current ?? credit.PostedBalance;
                    credit.CreditLimit = plaid.Limit ?? credit.CreditLimit;
                    break;

                case InvestmentAccount invest:
                    invest.CashBalance = plaid.Current ?? invest.CashBalance;
                    break;
            }
        }

        private void UpdateMetadata(Account account, AccountDto plaid)
        {
            account.AccountNumber = plaid.Mask;
            account.Institution = plaid.Name;
            // Additional metadata can be added later
        }
    }

    public class AccountSyncResult
    {
        public Guid AccountId { get; set; }

        public decimal? PostedBalance { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal? CashBalance { get; set; }
        public decimal? Principal { get; set; }
    }
}
