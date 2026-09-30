using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators
{
    public class AccountTransactionFileImportOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly AccountTransactionFileReader _reader;
        private readonly Categorizer _categorizer;
        private readonly TransactionUpdaterOrchestrator _ledger;

        public AccountTransactionFileImportOrchestrator()
            : this(new DataStoreFactory().GetAccountStore(), new DataStoreFactory().GetTransactionStore())
        {
        }

        public AccountTransactionFileImportOrchestrator(
            IAccountDataStore accounts,
            ITransactionDataStore transactions)
        {
            _accounts = accounts;
            _transactions = transactions;
            _reader = new AccountTransactionFileReader();
            _categorizer = new Categorizer(transactions as ICategoryDataStore ?? new DataStoreFactory().GetCategoryStore());
            _ledger = new TransactionUpdaterOrchestrator(accounts, transactions);
        }

        public TransactionFileColumnMap? GetMap(Guid accountId) =>
            _accounts.GetTransactionFileColumnMap(accountId);

        public void SaveMap(TransactionFileColumnMap map)
        {
            ArgumentNullException.ThrowIfNull(map);
            _accounts.UpsertTransactionFileColumnMap(map);
        }

        public TransactionFileSheet Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A file path is required.");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The selected file was not found.", filePath);
            return _reader.Read(filePath);
        }

        public static TransactionFileColumnMap SuggestMap(Guid accountId, IReadOnlyList<string> headers) =>
            TransactionFileColumnMapping.Suggest(accountId, headers);

        public IReadOnlyList<TransactionFileImportRow> BuildPreview(
            Guid accountId,
            TransactionFileSheet sheet,
            TransactionFileColumnMap map)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(map);
            var error = TransactionFileColumnMapping.Validate(sheet.Headers, map);
            if (error is not null)
                throw new InvalidOperationException(error);

            var seen = _transactions.GetPostedTransactions(accountId)
                .Concat<PostedTransaction>(_transactions.GetPostedTransferTransactions(accountId))
                .Select(posted => Key(posted.Date, posted.Amount, posted.Description))
                .ToHashSet();

            var rows = new List<TransactionFileImportRow>();
            foreach (var mapped in _reader.Apply(sheet, map))
            {
                var duplicate = false;
                if (mapped.Problem is null && mapped.Date is DateTime date && mapped.Amount is decimal amount)
                    duplicate = !seen.Add(Key(date, amount, mapped.Description));

                rows.Add(new TransactionFileImportRow
                {
                    SourceRow = mapped.SourceRow,
                    Date = mapped.Date,
                    Description = mapped.Description,
                    Amount = mapped.Amount,
                    Problem = mapped.Problem,
                    IsDuplicate = duplicate,
                    Import = mapped.Problem is null && !duplicate
                });
            }

            return rows;
        }

        public ImportResult Import(Guid accountId, IEnumerable<TransactionFileImportRow> rows) =>
            Import(accountId, rows, progress: null);

        public ImportResult Import(
            Guid accountId,
            IEnumerable<TransactionFileImportRow> rows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(rows);
            var selected = rows
                .Where(row => row.Import && row.Problem is null && row.Date is not null && row.Amount is not null)
                .ToList();

            var total = selected.Count;
            ImportProgressReporter.Report(progress, 0, total, activity: "Importing transactions");
            for (var i = 0; i < selected.Count; i++)
            {
                var row = selected[i];
                var posted = new PostedTransaction
                {
                    AccountId = accountId,
                    Date = row.Date!.Value,
                    Amount = row.Amount!.Value,
                    Description = row.Description ?? "",
                    ImportedStatus = ImportedStatus.Unreconciled
                };
                _categorizer.ApplySuggestion(posted);
                _transactions.AddPostedTransaction(posted);
                ImportProgressReporter.Report(progress, i + 1, total, activity: "Importing transactions");
            }

            if (total > 0)
            {
                ImportProgressReporter.Report(progress, total, total, ImportProgress.LedgerPhase);
                _ledger.RunLedgerUpdate();
            }

            ImportProgressReporter.Report(progress, total, total);
            return ImportResult.FromDates(total, selected.Select(row => row.Date!.Value));
        }

        private static DuplicateKey Key(DateTime date, decimal amount, string? description) =>
            new(date.Date, amount, (description ?? "").Trim().ToUpperInvariant());

        private readonly record struct DuplicateKey(DateTime Date, decimal Amount, string Description);
    }
}
