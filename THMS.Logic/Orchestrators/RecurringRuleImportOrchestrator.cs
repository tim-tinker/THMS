using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators
{
    public class RecurringRuleImportOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly RecurringRuleOrchestrator _rules;
        private readonly Categorizer _categorizer;
        private readonly SpreadsheetRecurringRuleImporter _importer;

        public RecurringRuleImportOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore())
        {
        }

        public RecurringRuleImportOrchestrator(IAccountDataStore accounts, ITransactionDataStore transactions)
            : this(
                accounts,
                new RecurringRuleOrchestrator(transactions),
                new Categorizer(transactions as ICategoryDataStore ?? new DataStoreFactory().GetCategoryStore()),
                new SpreadsheetRecurringRuleImporter(accounts))
        {
        }

        public RecurringRuleImportOrchestrator(
            IAccountDataStore accounts,
            RecurringRuleOrchestrator rules,
            Categorizer categorizer,
            SpreadsheetRecurringRuleImporter importer)
        {
            _accounts = accounts;
            _rules = rules;
            _categorizer = categorizer;
            _importer = importer;
        }

        public List<RecurringRuleImportPreview> LoadRulesFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A file path is required.");
            if (!File.Exists(path))
                throw new FileNotFoundException("The selected file was not found.", path);

            return _importer.Parse(path).Select(RecurringRuleImportPreview.FromParsed).ToList();
        }

        public ImportResult ImportRules(IEnumerable<RecurringRuleImportPreview> previewRows) =>
            ImportRules(previewRows, progress: null);

        public ImportResult ImportRules(
            IEnumerable<RecurringRuleImportPreview> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows as IReadOnlyList<RecurringRuleImportPreview> ?? previewRows.ToList();
            var imported = 0;
            ImportProgressReporter.Report(progress, 0, rows.Count, stride: 1);

            for (var i = 0; i < rows.Count; i++)
            {
                if (TryImport(rows[i]))
                    imported++;
                ImportProgressReporter.Report(progress, i + 1, rows.Count, stride: 1);
            }

            return ImportResult.CountOnly(imported);
        }

        private bool TryImport(RecurringRuleImportPreview row)
        {
            var accountName = row.Account?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(row.Description))
                return false;

            var account = _accounts.GetAccount(accountName)
                ?? _accounts.GetAllAccounts().FirstOrDefault(a =>
                    string.Equals(a.Name, accountName, StringComparison.OrdinalIgnoreCase));
            if (account is null)
                return false;

            if (!SpreadsheetRecurringRuleImporter.TryParseFrequency(row.Frequency, out var frequency))
                throw new InvalidOperationException($"Unknown frequency '{row.Frequency}' for '{row.Description}'.");

            var rule = new RecurringSingleTransactionRule
            {
                AccountId = account.Id,
                Description = row.Description.Trim(),
                Amount = row.Amount,
                Frequency = frequency,
                NextOccurrence = row.NextOccurrence == default ? DateTime.Today : row.NextOccurrence.Date,
                LastOccurrence = row.LastOccurrence?.Date,
                IsActive = true,
                IsUserCreated = true
            };

            var categoryName = row.Category?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(categoryName))
                rule.ApplyCategory(_categorizer.GetOrCreate(categoryName));

            _rules.AddSingleRule(rule);
            return true;
        }
    }
}
