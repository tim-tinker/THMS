using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators
{
    public class RecurringTransferImportOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly RecurringRuleOrchestrator _rules;
        private readonly Categorizer _categorizer;
        private readonly SpreadsheetRecurringTransferImporter _importer;

        public RecurringTransferImportOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore())
        {
        }

        public RecurringTransferImportOrchestrator(IAccountDataStore accounts, ITransactionDataStore transactions)
            : this(
                accounts,
                new RecurringRuleOrchestrator(transactions),
                new Categorizer(transactions as ICategoryDataStore ?? new DataStoreFactory().GetCategoryStore()),
                new SpreadsheetRecurringTransferImporter(accounts))
        {
        }

        public RecurringTransferImportOrchestrator(
            IAccountDataStore accounts,
            RecurringRuleOrchestrator rules,
            Categorizer categorizer,
            SpreadsheetRecurringTransferImporter importer)
        {
            _accounts = accounts;
            _rules = rules;
            _categorizer = categorizer;
            _importer = importer;
        }

        public List<RecurringTransferImportPreview> LoadRulesFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A file path is required.");
            if (!File.Exists(path))
                throw new FileNotFoundException("The selected file was not found.", path);

            return _importer.Parse(path).Select(RecurringTransferImportPreview.FromParsed).ToList();
        }

        public ImportResult ImportRules(IEnumerable<RecurringTransferImportPreview> previewRows) =>
            ImportRules(previewRows, progress: null);

        public ImportResult ImportRules(
            IEnumerable<RecurringTransferImportPreview> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows as IReadOnlyList<RecurringTransferImportPreview> ?? previewRows.ToList();
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

        private bool TryImport(RecurringTransferImportPreview row)
        {
            if (string.IsNullOrWhiteSpace(row.Description))
                return false;

            var from = ResolveAccount(row.FromAccount);
            var to = ResolveAccount(row.ToAccount);
            if (from is null || to is null || from.Id == to.Id)
                return false;

            if (!SpreadsheetRecurringRuleImporter.TryParseFrequency(row.Frequency, out var frequency))
                throw new InvalidOperationException($"Unknown frequency '{row.Frequency}' for '{row.Description}'.");

            var rule = new RecurringTransferRule
            {
                FromAccountId = from.Id,
                ToAccountId = to.Id,
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

            _rules.AddTransferRule(rule);
            return true;
        }

        private Account? ResolveAccount(string? name)
        {
            var trimmed = name?.Trim() ?? "";
            if (trimmed.Length == 0)
                return null;

            return _accounts.GetAccount(trimmed)
                ?? _accounts.GetAllAccounts().FirstOrDefault(a =>
                    string.Equals(a.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        }
    }
}
