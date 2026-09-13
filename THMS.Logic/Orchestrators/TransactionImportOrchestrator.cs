using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.External;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.Mapping;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators
{
    public class TransactionImportOrchestrator
    {
        private readonly IExternalTransactionFetcher _transactionFetcher;
        private readonly ITransactionDataStore _txStore;
        private readonly SpreadsheetTransactionImporter _spreadsheetImporter;
        private readonly TransactionUpdaterOrchestrator _ledgerUpdater;
        private readonly Categorizer _categorizer;
        private double _dateWindowSize = 3; // use three because of weekends

        public TransactionImportOrchestrator()
            : this(
                new ExternalFetcherFactory().GetTransactionFetcher(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetAccountStore())
        {
        }

        public TransactionImportOrchestrator(
            IExternalTransactionFetcher transactionFetcher,
            ITransactionDataStore txStore)
            : this(transactionFetcher, txStore, new DataStoreFactory().GetAccountStore())
        {
        }

        public TransactionImportOrchestrator(
            IExternalTransactionFetcher transactionFetcher,
            ITransactionDataStore txStore,
            IAccountDataStore accountStore)
            : this(
                transactionFetcher,
                txStore,
                new SpreadsheetTransactionImporter(txStore, accountStore),
                new TransactionUpdaterOrchestrator(accountStore, txStore))
        {
        }

        public TransactionImportOrchestrator(
            IExternalTransactionFetcher transactionFetcher,
            ITransactionDataStore txStore,
            SpreadsheetTransactionImporter spreadsheetImporter,
            TransactionUpdaterOrchestrator ledgerUpdater)
        {
            _transactionFetcher = transactionFetcher;
            _txStore = txStore;
            _spreadsheetImporter = spreadsheetImporter;
            _ledgerUpdater = ledgerUpdater;
            _categorizer = new Categorizer(txStore as ICategoryDataStore ?? new DataStoreFactory().GetCategoryStore());
        }

        public List<TransactionImportPreview> LoadTransactionsFromFiles(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            var rows = new List<TransactionImportPreview>();
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("A file path is required.");
                if (!File.Exists(path))
                    throw new FileNotFoundException("The selected file was not found.", path);

                foreach (var parsed in _spreadsheetImporter.Parse(path))
                {
                    rows.Add(new TransactionImportPreview
                    {
                        Date = parsed.Transaction.Date,
                        Description = parsed.Transaction.Description ?? "",
                        Amount = parsed.Transaction.Amount,
                        Account = parsed.AccountName,
                        Category = parsed.CategoryName,
                        AccountId = parsed.Transaction.AccountId
                    });
                }
            }

            return rows;
        }

        public int ImportTransactions(IEnumerable<TransactionImportPreview> previewRows) =>
            ImportTransactions(previewRows, progress: null);

        public int ImportTransactions(
            IEnumerable<TransactionImportPreview> previewRows,
            IProgress<TransactionImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows as IReadOnlyList<TransactionImportPreview> ?? previewRows.ToList();
            var existingByAccount = new Dictionary<Guid, List<PostedTransaction>>();
            var toImport = new List<TransactionImportPreview>();

            foreach (var row in rows)
            {
                if (row.AccountId == Guid.Empty || IsDuplicate(row, existingByAccount))
                    continue;

                toImport.Add(row);
                existingByAccount[row.AccountId].Add(ToPosted(row, categorize: false));
            }

            var total = toImport.Count;
            Report(progress, 0, total);

            for (var i = 0; i < toImport.Count; i++)
            {
                var posted = ToPosted(toImport[i], categorize: true);
                _txStore.AddPostedTransaction(posted);
                Report(progress, i + 1, total);
            }

            if (total > 0)
            {
                Report(progress, total, total, updatingLedger: true);
                _ledgerUpdater.RunLedgerUpdate();
            }

            Report(progress, total, total);
            return total;
        }

        private PostedTransaction ToPosted(TransactionImportPreview row, bool categorize)
        {
            var posted = new PostedTransaction
            {
                AccountId = row.AccountId,
                Date = row.Date,
                Amount = row.Amount,
                Description = row.Description ?? ""
            };

            if (!categorize)
                return posted;

            if (!string.IsNullOrWhiteSpace(row.Category))
                posted.ApplyCategory(_categorizer.GetOrCreate(row.Category));
            else
                _categorizer.ApplySuggestion(posted);

            return posted;
        }

        private static void Report(
            IProgress<TransactionImportProgress>? progress,
            int completed,
            int total,
            bool updatingLedger = false)
        {
            if (progress is null)
                return;
            if (!updatingLedger && completed != 0 && completed != total && completed % 25 != 0)
                return;

            progress.Report(new TransactionImportProgress(completed, total, updatingLedger));
        }

        public async Task<TransactionImportResult> ImportAsync(Account account)
        {
            if (account.ExternalLink is null)
                throw new InvalidOperationException("Account is not linked to Plaid.");

            var accountDto = account.ToDto();

            // 1. Fetch Plaid transactions
            var plaidTxs = await _transactionFetcher.FetchTransactionsAsync(
                accountDto,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow);

            // 2. Filter out pending
            var postedDtos = plaidTxs.Where(t => !t.Pending).ToList();

            // 3. Map to domain
            var posted = postedDtos.Select(dto => MapPosted(dto, account.Id)).ToList();

            // 4. Detect transfers
            var transfers = DetectTransfers(posted);

            Categorize(posted);

            // 6. Insert posted transactions
            foreach (var p in posted)
                _txStore.AddPostedTransaction(p);

            return new TransactionImportResult
            {
                AccountId = account.Id,
                PostedImported = posted.Count,
                TransfersDetected = transfers.Count
            };
        }

        private bool IsDuplicate(
            TransactionImportPreview row,
            Dictionary<Guid, List<PostedTransaction>> existingByAccount)
        {
            if (!existingByAccount.TryGetValue(row.AccountId, out var existing))
            {
                existing = _txStore.GetPostedTransactions(row.AccountId).ToList();
                existingByAccount[row.AccountId] = existing;
            }

            return existing.Any(posted =>
                posted.Date.Date == row.Date.Date
                && posted.Amount == row.Amount
                && string.Equals(posted.Description ?? "", row.Description ?? "", StringComparison.OrdinalIgnoreCase));
        }

        private PostedTransaction MapPosted(TransactionDto dto, Guid accountId)
        {
            return new PostedTransaction
            {
                AccountId = accountId,
                Amount = dto.Amount,
                Date = dto.Date ?? DateTime.UtcNow,
                Description = dto.Name,
                PlaidCategory = dto.Category
            };
        }

        private List<TransferTransaction> DetectTransfers(
            List<PostedTransaction> imported)
        {
            var results = new List<TransferTransaction>();

            if (0 < imported.Count)
            {
                var existing = GetExistingTransactions(imported);

                // Combine imported + existing for matching
                var all = existing.Concat(imported).ToList();

                // Group by absolute amount for fast lookup
                var byAmount = all
                    .GroupBy(t => Math.Abs(t.Amount))
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var t in imported)
                {
                    var amount = Math.Abs(t.Amount);

                    if (!byAmount.TryGetValue(amount, out var candidates))
                        continue;

                    foreach (var c in candidates)
                    {
                        // Skip self
                        if (IsTransactionMatched(t, c))
                        {
                            // We found a transfer pair
                            var debit = t.Amount < 0 ? t : c;
                            var credit = t.Amount > 0 ? t : c;

                            // Convert both to PostedTransferTransaction
                            var debitTransfer = new PostedTransferTransaction(debit, credit.Id, TransferDirection.Outgoing);

                            var creditTransfer = new PostedTransferTransaction(credit, debit.Id, TransferDirection.Incoming);

                            // Replace in ledger
                            _txStore.ReplacePostedTransaction(debitTransfer);
                            _txStore.ReplacePostedTransaction(creditTransfer);

                            // Create conceptual TransferTransaction
                            results.Add(new TransferTransaction
                            {
                                FromAccountId = debit.AccountId,
                                ToAccountId = credit.AccountId,
                                Amount = Math.Abs(debit.Amount),
                                Date = debit.Date
                            });

                            // Once matched, stop scanning this t
                            break;
                        }
                    }
                }
            }

            return results;
        }

        private IEnumerable<PostedTransaction> GetExistingTransactions(List<PostedTransaction> imported)
        {
            var sortedImported = (from tx in imported orderby tx.Date select tx).ToList();
            var start = sortedImported.First().Date.AddDays(-_dateWindowSize);
            var end = sortedImported.Last().Date.AddDays(_dateWindowSize);

            return _txStore.GetPostedTransactions(start, end);
        }

        private bool IsTransactionMatched(PostedTransaction t, PostedTransaction c)
        {
            var isMatched = false;
            // Skip self
            if (t.Id != c.Id)
            {
                // Must be opposite signs
                if (0 < t.Amount * c.Amount)
                {
                    // Must be different accounts
                    if (t.AccountId != c.AccountId)
                    {
                        // Must be within date window
                        if (IsWithinDateWindow(t.Date, c.Date))
                        {
                            // Optional: description heuristic
                            if (LooksLikeTransfer(t.Description, c.Description))
                            {
                                isMatched = true;
                            }
                        }
                    }
                }
            }

            return isMatched;
        }

        private bool IsWithinDateWindow(DateTime d1, DateTime d2)
        {
            return Math.Abs((d1.Date - d2.Date).TotalDays) <= _dateWindowSize;
        }

        private bool LooksLikeTransfer(string? d1, string? d2)
        {
            var text = (d1 + " " + d2).ToUpperInvariant();
            return text.Contains("TRANSFER")
                || text.Contains("ACH")
                || text.Contains("ONLINE")
                || text.Contains("PAYMENT");
        }

        private void Categorize(IEnumerable<PostedTransaction> txs)
        {
            foreach (var tx in txs)
                _categorizer.ApplySuggestion(tx);
        }

        public class TransactionImportResult
        {
            public Guid AccountId { get; set; }
            public int PostedImported { get; set; }
            public int TransfersDetected { get; set; }
        }
    }

    public readonly record struct TransactionImportProgress(int Completed, int Total, bool UpdatingLedger = false);
}
