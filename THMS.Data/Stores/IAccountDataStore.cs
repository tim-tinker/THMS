using THMS.Domain.Finance.Accounts;

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
    }
}
