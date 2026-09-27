using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.External;
using THMS.Logic.Mapping;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators.Finance
{
    public sealed class PlaidSyncProgress
    {
        public string Message { get; init; } = "";
        public bool Complete { get; init; }
    }

    public sealed class PlaidSyncResult
    {
        public int Imported { get; init; }
        public int ItemsSynced { get; init; }
        public int ItemsNeedingAuth { get; init; }
        public int ItemsSkipped { get; init; }

        public string Summary
        {
            get
            {
                if (ItemsNeedingAuth > 0 && Imported == 0 && ItemsSynced == 0)
                    return "Plaid needs sign-in for one or more institutions.";
                if (Imported == 0 && ItemsSynced == 0 && ItemsSkipped > 0)
                    return "Plaid is linked. Import history from Link Accounts before auto-sync can run.";
                if (Imported == 0)
                    return ItemsNeedingAuth > 0
                        ? $"Plaid sync finished. {ItemsNeedingAuth} institution{(ItemsNeedingAuth == 1 ? "" : "s")} need sign-in."
                        : "Plaid is up to date.";
                var text = $"Imported {Imported:N0} Plaid transaction{(Imported == 1 ? "" : "s")}.";
                if (ItemsNeedingAuth > 0)
                    text += $" {ItemsNeedingAuth} institution{(ItemsNeedingAuth == 1 ? "" : "s")} need sign-in.";
                return text;
            }
        }
    }

    public class PlaidTransactionOrchestrator
    {
        private static readonly SemaphoreSlim SyncGate = new(1, 1);

        private readonly IAccountDataStore _accounts;
        private readonly IExternalTransactionFetcher _transactionFetcher;
        private readonly ITransactionDataStore _transactions;
        private readonly TransactionImportOrchestrator _importOrchestrator;

        public PlaidTransactionOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new ExternalFetcherFactory().GetTransactionFetcher(),
                new DataStoreFactory().GetTransactionStore())
        {
        }

        public PlaidTransactionOrchestrator(
            IAccountDataStore accounts,
            IExternalTransactionFetcher transactionFetcher,
            ITransactionDataStore transactions)
            : this(accounts, transactionFetcher, transactions, new TransactionImportOrchestrator(transactionFetcher, transactions, accounts))
        {
        }

        public PlaidTransactionOrchestrator(
            IAccountDataStore accounts,
            IExternalTransactionFetcher transactionFetcher,
            TransactionImportOrchestrator importOrchestrator)
            : this(accounts, transactionFetcher, new DataStoreFactory().GetTransactionStore(), importOrchestrator)
        {
        }

        public PlaidTransactionOrchestrator(
            IAccountDataStore accounts,
            IExternalTransactionFetcher transactionFetcher,
            ITransactionDataStore transactions,
            TransactionImportOrchestrator importOrchestrator)
        {
            _accounts = accounts;
            _transactionFetcher = transactionFetcher;
            _transactions = transactions;
            _importOrchestrator = importOrchestrator;
        }

        public bool HasLinkedItems() => LinkedItems().Count > 0;

        public bool HasItemsReadyForIncrementalSync() =>
            LinkedItems().Any(item => ReadyForIncremental(item.ItemId));

        public async Task<PlaidSyncResult> SyncIncrementalAsync(
            CancellationToken cancellationToken = default,
            IProgress<PlaidSyncProgress>? progress = null)
        {
            await SyncGate.WaitAsync(cancellationToken);
            try
            {
                var imported = 0;
                var synced = 0;
                var auth = 0;
                var skipped = 0;
                foreach (var item in LinkedItems())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!ReadyForIncremental(item.ItemId))
                    {
                        skipped++;
                        continue;
                    }

                    progress?.Report(new PlaidSyncProgress { Message = $"Syncing Plaid ({item.Institution})..." });
                    var count = await SyncItemAsync(item, historyStart: null, cancellationToken, progress);
                    if (count < 0)
                        auth++;
                    else
                    {
                        synced++;
                        imported += count;
                    }
                }

                progress?.Report(new PlaidSyncProgress { Message = "Plaid sync finished.", Complete = true });
                return new PlaidSyncResult
                {
                    Imported = imported,
                    ItemsSynced = synced,
                    ItemsNeedingAuth = auth,
                    ItemsSkipped = skipped
                };
            }
            finally
            {
                SyncGate.Release();
            }
        }

        public async Task<PlaidSyncResult> SyncInitialAsync(
            string itemId,
            DateTime historyStart,
            CancellationToken cancellationToken = default,
            IProgress<PlaidSyncProgress>? progress = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
            await SyncGate.WaitAsync(cancellationToken);
            try
            {
                var item = LinkedItems().FirstOrDefault(i => i.ItemId == itemId)
                    ?? throw new InvalidOperationException("That Plaid institution is not linked.");
                progress?.Report(new PlaidSyncProgress { Message = $"Importing Plaid history ({item.Institution})..." });
                var count = await SyncItemAsync(item, historyStart.Date, cancellationToken, progress);
                progress?.Report(new PlaidSyncProgress { Message = "Plaid history import finished.", Complete = true });
                if (count < 0)
                    return new PlaidSyncResult { ItemsNeedingAuth = 1 };

                return new PlaidSyncResult { Imported = count, ItemsSynced = 1 };
            }
            finally
            {
                SyncGate.Release();
            }
        }

        public async Task<List<PlaidTransactionViewModel>> DownloadNewTransactions(DateTime start, DateTime end)
        {
            var linked = _accounts.GetAllAccounts()
                .Where(HasPlaidLink)
                .ToList();
            if (linked.Count == 0)
                throw new InvalidOperationException(
                    "No Plaid-linked accounts. Connect accounts to Plaid first.");

            var results = new List<PlaidTransactionViewModel>();
            foreach (var group in linked.GroupBy(a => a.ExternalLink!.AccessToken))
            {
                var representative = group.First();
                var dtos = await _transactionFetcher.FetchTransactionsAsync(
                    representative.ToDto(),
                    start,
                    end);

                foreach (var dto in dtos)
                {
                    var match = group.FirstOrDefault(a =>
                        string.Equals(a.ExternalLink!.PlaidAccountId, dto.AccountId, StringComparison.Ordinal));
                    if (match is null)
                        continue;

                    var date = dto.Date ?? start;
                    if (date.Date < start.Date || date.Date > end.Date)
                        continue;

                    results.Add(new PlaidTransactionViewModel
                    {
                        Date = date,
                        Description = dto.Name,
                        Amount = dto.Amount,
                        Account = match.Name,
                        Category = dto.Category ?? "",
                        Pending = dto.Pending,
                        AccountId = match.Id,
                        PlaidAccountId = dto.AccountId
                    });
                }
            }

            return results.OrderBy(r => r.Date).ThenBy(r => r.Description).ToList();
        }

        public ImportResult ImportTransactions(IEnumerable<PlaidTransactionViewModel> previewRows) =>
            ImportTransactions(previewRows, progress: null);

        public ImportResult ImportTransactions(
            IEnumerable<PlaidTransactionViewModel> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var mapped = previewRows
                .Where(row => !row.Pending)
                .Select(row => new TransactionImportPreview
                {
                    Date = row.Date,
                    Description = row.Description,
                    Amount = row.Amount,
                    Account = row.Account,
                    Category = row.Category,
                    AccountId = row.AccountId
                });

            return _importOrchestrator.ImportTransactions(mapped, progress);
        }

        private async Task<int> SyncItemAsync(
            LinkedItem item,
            DateTime? historyStart,
            CancellationToken cancellationToken,
            IProgress<PlaidSyncProgress>? progress)
        {
            var state = _accounts.GetPlaidItemSyncState(item.ItemId) ?? new PlaidItemSyncState { ItemId = item.ItemId };
            state.Status = PlaidItemSyncStatus.Syncing;
            state.LastError = "";
            if (historyStart is DateTime start)
                state.HistoryStartDate = start;
            _accounts.UpsertPlaidItemSyncState(state);

            var imported = 0;
            var cursor = historyStart is null ? state.Cursor : "";
            var verb = historyStart is null ? "Syncing Plaid" : "Importing Plaid history";
            try
            {
                var hasMore = true;
                while (hasMore)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TransactionSyncPage page;
                    try
                    {
                        page = await _transactionFetcher.SyncTransactionsAsync(item.AccessToken, cursor, cancellationToken);
                    }
                    catch (InvalidOperationException ex) when (IsMutationDuringPagination(ex))
                    {
                        cursor = state.Cursor;
                        progress?.Report(new PlaidSyncProgress
                        {
                            Message = $"{verb} ({item.Institution})…"
                        });
                        continue;
                    }

                    imported += ApplyPage(item, page, historyStart ?? state.HistoryStartDate);
                    cursor = page.NextCursor;
                    state.Cursor = cursor;
                    _accounts.UpsertPlaidItemSyncState(state);
                    hasMore = page.HasMore;
                    progress?.Report(new PlaidSyncProgress
                    {
                        Message = hasMore
                            ? $"{verb} ({item.Institution})… {imported:N0} imported"
                            : $"{verb} ({item.Institution})…"
                    });
                }

                state.Status = PlaidItemSyncStatus.Synced;
                state.LastSyncedAt = DateTime.UtcNow;
                state.LastError = "";
                _accounts.UpsertPlaidItemSyncState(state);
                return imported;
            }
            catch (PlaidItemLoginRequiredException ex)
            {
                state.Status = PlaidItemSyncStatus.NeedsAuth;
                state.LastError = ex.Message;
                _accounts.UpsertPlaidItemSyncState(state);
                return -1;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                state.Status = string.IsNullOrWhiteSpace(state.Cursor)
                    ? PlaidItemSyncStatus.NotSynced
                    : PlaidItemSyncStatus.Synced;
                state.LastError = ex.Message;
                _accounts.UpsertPlaidItemSyncState(state);
                throw;
            }
        }

        private int ApplyPage(LinkedItem item, TransactionSyncPage page, DateTime? historyStart)
        {
            var byPlaidId = item.Accounts.ToDictionary(a => a.ExternalLink!.PlaidAccountId, StringComparer.Ordinal);
            var toImport = new List<TransactionImportPreview>();
            foreach (var dto in page.Added.Concat(page.Modified))
            {
                if (dto.Pending || string.IsNullOrWhiteSpace(dto.TransactionId))
                    continue;
                if (!byPlaidId.TryGetValue(dto.AccountId, out var account))
                    continue;
                var date = dto.Date ?? DateTime.Today;
                if (historyStart is DateTime start && date.Date < start.Date)
                    continue;

                toImport.Add(new TransactionImportPreview
                {
                    Date = date,
                    Description = dto.Name,
                    Amount = dto.Amount,
                    Account = account.Name,
                    Category = dto.Category ?? "",
                    AccountId = account.Id,
                    ExternalTransactionId = dto.TransactionId
                });
            }

            var imported = 0;
            if (toImport.Count > 0)
                imported = _importOrchestrator.ImportTransactions(toImport).Count;

            foreach (var removedId in page.RemovedIds)
                TryRemoveUnreconciled(removedId);

            return imported;
        }

        private void TryRemoveUnreconciled(string externalId)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return;

            foreach (var posted in _transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue)
                         .Where(p => string.Equals(p.ExternalTransactionId, externalId, StringComparison.Ordinal)
                             && p.ImportedStatus == ImportedStatus.Unreconciled)
                         .ToList())
            {
                _transactions.DeletePostedTransaction(posted.Id);
            }

            foreach (var posted in _transactions.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue)
                         .Where(p => string.Equals(p.ExternalTransactionId, externalId, StringComparison.Ordinal)
                             && p.ImportedStatus == ImportedStatus.Unreconciled)
                         .ToList())
            {
                _transactions.DeletePostedTransferTransaction(posted.Id);
            }
        }

        private bool ReadyForIncremental(string itemId)
        {
            var state = _accounts.GetPlaidItemSyncState(itemId);
            return state is not null
                && state.HasCursor
                && state.Status != PlaidItemSyncStatus.NeedsAuth;
        }

        private List<LinkedItem> LinkedItems()
        {
            return _accounts.GetAllAccounts()
                .Where(HasPlaidLink)
                .GroupBy(a => a.ExternalLink!.ItemId, StringComparer.Ordinal)
                .Select(group =>
                {
                    var first = group.First().ExternalLink!;
                    return new LinkedItem
                    {
                        ItemId = group.Key,
                        AccessToken = first.AccessToken,
                        Institution = string.IsNullOrWhiteSpace(first.InstitutionName)
                            ? group.First().Institution
                            : first.InstitutionName,
                        Accounts = group.ToList()
                    };
                })
                .ToList();
        }

        private static bool HasPlaidLink(Account account) =>
            account.ExternalLink is { } link
            && !string.IsNullOrWhiteSpace(link.AccessToken)
            && !string.IsNullOrWhiteSpace(link.PlaidAccountId)
            && !string.IsNullOrWhiteSpace(link.ItemId);

        private static bool IsMutationDuringPagination(InvalidOperationException ex) =>
            ex.Message.Contains("TRANSACTIONS_SYNC_MUTATION_DURING_PAGINATION", StringComparison.OrdinalIgnoreCase);

        private sealed class LinkedItem
        {
            public required string ItemId { get; init; }
            public required string AccessToken { get; init; }
            public required string Institution { get; init; }
            public required List<Account> Accounts { get; init; }
        }
    }
}
