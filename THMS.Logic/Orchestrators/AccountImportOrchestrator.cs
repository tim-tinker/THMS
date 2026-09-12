using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators.Finance
{
    public class AccountImportOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly SpreadsheetAccountImporter _importer;

        public AccountImportOrchestrator()
            : this(new DataStoreFactory().GetAccountStore())
        {
        }

        public AccountImportOrchestrator(IAccountDataStore accounts)
            : this(accounts, new SpreadsheetAccountImporter(accounts))
        {
        }

        public AccountImportOrchestrator(IAccountDataStore accounts, SpreadsheetAccountImporter importer)
        {
            _accounts = accounts;
            _importer = importer;
        }

        public List<AccountImportPreview> LoadAccountsFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A file path is required.");
            if (!File.Exists(path))
                throw new FileNotFoundException("The selected file was not found.", path);

            return _importer.Parse(path).Select(AccountImportPreview.FromAccount).ToList();
        }

        public int ImportAccounts(IEnumerable<AccountImportPreview> previewRows)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var existing = _accounts.GetAllAccounts().ToList();
            var imported = 0;

            foreach (var row in previewRows)
            {
                var account = row.ApplyToAccount();
                var match = existing.FirstOrDefault(a =>
                    string.Equals(a.Name, account.Name, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    account.Id = match.Id;

                _accounts.UpsertAccount(account);
                imported++;
            }

            return imported;
        }

        public IReadOnlyList<Account> GetAllAccounts() =>
            _accounts.GetAllAccounts().ToList();
    }
}
