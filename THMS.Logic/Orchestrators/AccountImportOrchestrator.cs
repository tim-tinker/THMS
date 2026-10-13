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

            return _importer.Parse(path).Select(parsed =>
            {
                var preview = AccountImportPreview.FromAccount(parsed.Account);
                preview.AutoPaySpecified = parsed.HasAutoPay;
                preview.AutoPay = parsed.Account.AutoPay;
                preview.FundingAccountSpecified = parsed.HasFundingAccount;
                preview.FundingAccountName = parsed.FundingAccountName;
                return preview;
            }).ToList();
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

            var imported = new List<Account>();
            for (var i = 0; i < rows.Count; i++)
            {
                var account = rows[i].ApplyToAccount();
                var match = existing.FirstOrDefault(a =>
                    string.Equals(a.Name, account.Name, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    account.Id = match.Id;
                    if (!rows[i].AutoPaySpecified)
                        account.AutoPay = match.AutoPay;
                    if (!rows[i].FundingAccountSpecified)
                        account.AutoPayFromAccountId = match.AutoPayFromAccountId;
                }

                _accounts.UpsertAccount(account);
                imported.Add(account);
                ImportProgressReporter.Report(progress, i + 1, total, stride: 1);
            }

            ApplyFundingAccounts(rows, imported);
            return ImportResult.CountOnly(total);
        }

        private void ApplyFundingAccounts(IReadOnlyList<AccountImportPreview> rows, IReadOnlyList<Account> imported)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var account = imported[i];
                if (!row.FundingAccountSpecified || !account.SupportsAutoPay)
                    continue;

                if (string.IsNullOrWhiteSpace(row.FundingAccountName))
                {
                    account.AutoPayFromAccountId = null;
                }
                else
                {
                    var funding = _accounts.GetAllAccounts().FirstOrDefault(candidate =>
                        string.Equals(candidate.Name, row.FundingAccountName, StringComparison.OrdinalIgnoreCase));
                    if (funding is null)
                        throw new InvalidOperationException(
                            $"Funding account '{row.FundingAccountName}' for '{account.Name}' was not found.");
                    if (funding.Id == account.Id)
                        throw new InvalidOperationException($"'{account.Name}' cannot pay from itself.");
                    account.AutoPayFromAccountId = funding.Id;
                }

                if (account.AutoPay && account.AutoPayFromAccountId is not Guid)
                    throw new InvalidOperationException($"Auto-pay for '{account.Name}' requires a funding account.");

                _accounts.UpsertAccount(account);
            }
        }

        public IReadOnlyList<Account> GetAllAccounts() =>
            _accounts.GetAllAccounts().ToList();
    }
}
