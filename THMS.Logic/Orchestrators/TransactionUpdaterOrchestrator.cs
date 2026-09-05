using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Transactions;

using THMS.Logic.Finance.Budget;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Recurrence;
using THMS.Logic.Finance.Transfer;

namespace THMS.Logic.Orchestrators
{
    public class TransactionUpdaterOrchestrator
    {
        private readonly IAccountDataStore _accountStore;
        private readonly ITransactionDataStore _transactionStore;

        private readonly TransferDetector _transferDetector = new();
        private readonly RecurringDetector _recurringDetector = new();
        private readonly ForecastGenerator _forecastGenerator = new();
        private readonly FutureReconciler _futureReconciler = new();

        private readonly ExpenseBudgetDetector _expenseBudgetDetector = new();

        private const int RecurrenceMonths = 13;
        private const int TransferLookbackDays = 3;
        private const int InactiveGraceDays = 30;

        public TransactionUpdaterOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore())
        {
        }

        public TransactionUpdaterOrchestrator(
            IAccountDataStore accountStore,
            ITransactionDataStore transactionStore)
        {
            _accountStore = accountStore;
            _transactionStore = transactionStore;
        }

        public UpdaterResult RunLedgerUpdate()
        {
            var result = new UpdaterResult();

            var accounts = _accountStore.GetAllAccounts().ToList();
            result.AccountsUpdated = accounts.Count;

            // ------------------------------------------------------------
            // 1. Determine active accounts for transfer detection
            // ------------------------------------------------------------
            var activeAccounts = new List<(Guid AccountId, DateTime LatestTransfer, DateTime LatestPosted)>();

            foreach (var account in accounts)
            {
                var latestPosted = _transactionStore.GetLatestPostedTransactionDate(account.Id);
                var latestTransfer = _transactionStore.GetLatestPostedTransferTransactionDate(account.Id);

                if (latestPosted is null || latestTransfer is null)
                    continue;

                if (latestPosted.Value >= latestTransfer.Value.AddDays(-InactiveGraceDays))
                {
                    activeAccounts.Add((account.Id, latestTransfer.Value, latestPosted.Value));
                }
            }

            // ------------------------------------------------------------
            // 2. Global transfer detection (active accounts only)
            // ------------------------------------------------------------
            if (activeAccounts.Any())
            {
                var earliestLatestTransfer = activeAccounts.Min(a => a.LatestTransfer);
                var transferStart = earliestLatestTransfer.AddDays(-TransferLookbackDays);
                var transferEnd = activeAccounts.Max(a => a.LatestPosted);

                var recentPosted = _transactionStore.GetPostedTransactions(transferStart, transferEnd).ToList();

                _transferDetector.DetectTransfers(recentPosted);

                foreach (var t in _transferDetector.Detected)
                    _transactionStore.AddPostedTransferTransaction(t);

                foreach (var m in _transferDetector.Matched)
                    _transactionStore.DeletePostedTransaction(m.Id);

                result.TransfersDetected = _transferDetector.Detected.Count;
            }

            // ------------------------------------------------------------
            // 3. Per-account recurrence detection, budgeting,
            //    forecasting, reconciliation, roll-off
            // ------------------------------------------------------------
            foreach (var account in accounts)
            {
                var latestPostedDate = _transactionStore.GetLatestPostedTransactionDate(account.Id);
                if (latestPostedDate is null)
                    continue;

                var recurrenceStart = latestPostedDate.Value.AddMonths(-RecurrenceMonths);

                var posted = _transactionStore.GetPostedTransactions(recurrenceStart, latestPostedDate.Value).ToList();
                var postedTransfers = _transactionStore.GetPostedTransferTransactions(recurrenceStart, latestPostedDate.Value).ToList();

                var existingSingleRules = _transactionStore.GetRecurringSingleRules(account.Id).ToList();
                var existingTransferRules = _transactionStore.GetRecurringTransferRules(account.Id).ToList();

                var existingBudgetRules = _transactionStore.GetExpenseBudgetRules(account.Id).ToList();

                // ------------------------------------------------------------
                // 3a. Detect recurring rules
                // ------------------------------------------------------------
                var newSingleRules = _recurringDetector.DetectRecurringSingles(posted, existingSingleRules);
                var newTransferRules = _recurringDetector.DetectRecurringTransfers(postedTransfers, existingTransferRules);

                result.RecurringRulesUpdated += newSingleRules.Count + newTransferRules.Count;

                var mergedSingleRules = existingSingleRules.Concat(newSingleRules).ToList();
                var mergedTransferRules = existingTransferRules.Concat(newTransferRules).ToList();

                // ------------------------------------------------------------
                // 3b. Detect/update all expense budget rules (including utilities)
                // ------------------------------------------------------------
                var updatedBudgetRules = new List<ExpenseBudgetRule>();

                foreach (var rule in existingBudgetRules)
                {
                    var updated = _expenseBudgetDetector.Detect(
                        account.Id,
                        posted,
                        rule,
                        rule.IncludedCategories);

                    updatedBudgetRules.Add(updated);
                    _transactionStore.UpsertExpenseBudgetRule(updated);
                }

                // ------------------------------------------------------------
                // 3c. Forecast future transactions
                // ------------------------------------------------------------
                var futureSingles = _forecastGenerator.GenerateFutureSingles(mergedSingleRules);
                var futureTransfers = _forecastGenerator.GenerateFutureTransfers(mergedTransferRules);

                var futureBudgets = updatedBudgetRules
                    .SelectMany(r => _forecastGenerator.GenerateExpenseBudgetForecast(r))
                    .ToList();

                foreach (var f in futureSingles)
                    _transactionStore.AddFutureSingleTransaction(f);

                foreach (var f in futureTransfers)
                    _transactionStore.AddFutureTransferTransaction(f);

                foreach (var f in futureBudgets)
                    _transactionStore.AddFutureSingleTransaction(f);

                result.ForecastUpdated = true;

                // ------------------------------------------------------------
                // 3d. Persist updated rules
                // ------------------------------------------------------------
                foreach (var r in mergedSingleRules)
                    _transactionStore.UpdateRecurringSingleRule(r);

                foreach (var r in mergedTransferRules)
                    _transactionStore.UpdateRecurringTransferRule(r);

                // Budget rules already persisted above

                // ------------------------------------------------------------
                // 3e. Reconcile future transactions (with tolerance)
                // ------------------------------------------------------------
                var allPostedNow = _transactionStore.GetPostedTransactions(account.Id).ToList();
                var allPostedTransfersNow = _transactionStore.GetPostedTransferTransactions(account.Id).ToList();

                var allFutureSingles = _transactionStore.GetFutureSingleTransactions(account.Id).ToList();
                var allFutureTransfers = _transactionStore.GetFutureTransferTransactions(account.Id).ToList();

                _futureReconciler.ReconcileSingles(allPostedNow, allFutureSingles, dayTolerance: 4);
                _futureReconciler.ReconcileTransfers(allPostedTransfersNow, allFutureTransfers, dayTolerance: 4);

                foreach (var f in _futureReconciler.MatchedSingles)
                    _transactionStore.UpdateFutureSingleTransaction(f);

                foreach (var f in _futureReconciler.MatchedTransfers)
                    _transactionStore.UpdateFutureTransferTransaction(f);

                // ------------------------------------------------------------
                // 3f. Roll-off realized future items
                // ------------------------------------------------------------
                foreach (var f in allFutureSingles.Where(f => f.IsRealized))
                    _transactionStore.DeleteFutureSingleTransaction(f.Id);

                foreach (var f in allFutureTransfers.Where(f => f.IsRealized))
                    _transactionStore.DeleteFutureTransferTransaction(f.Id);

                result.RollOffCompleted = true;
            }

            return result;
        }
    }
}
