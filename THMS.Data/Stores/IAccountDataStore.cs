using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores
{
    public interface IAccountDataStore
    {
        // Create or update an account
        void UpsertAccount(Account account);

        // Retrieve a single account by Name
        Account? GetAccount(string name);

        // Retrieve all accounts
        IEnumerable<Account> GetAllAccounts();
        void DeleteAccount(Guid id);

        IReadOnlyList<PlaidItemSyncState> GetPlaidItemSyncStates();
        PlaidItemSyncState? GetPlaidItemSyncState(string itemId);
        void UpsertPlaidItemSyncState(PlaidItemSyncState state);

        TransactionFileColumnMap? GetTransactionFileColumnMap(Guid accountId);
        void UpsertTransactionFileColumnMap(TransactionFileColumnMap map);
    }
}
