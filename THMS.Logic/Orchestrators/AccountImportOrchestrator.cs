using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.ViewModels;
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

        public ImportResult ImportAccounts(IEnumerable<AccountImportPreview> previewRows) =>
            ImportAccounts(previewRows, progress: null);

        public ImportResult ImportAccounts(
            IEnumerable<AccountImportPreview> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows as IReadOnlyList<AccountImportPreview> ?? previewRows.ToList();
            var existing = _accounts.GetAllAccounts().ToList();
            var total = rows.Count;
            ImportProgressReporter.Report(progress, 0, total, stride: 1);

            for (var i = 0; i < rows.Count; i++)
            {
                var account = rows[i].ApplyToAccount();
                var match = existing.FirstOrDefault(a =>
                    string.Equals(a.Name, account.Name, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    account.Id = match.Id;

                _accounts.UpsertAccount(account);
                ImportProgressReporter.Report(progress, i + 1, total, stride: 1);
            }

            return ImportResult.CountOnly(total);
        }

        public IReadOnlyList<Account> GetAllAccounts() =>
            _accounts.GetAllAccounts().ToList();
    }
}
