using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class TransactionUpdaterOrchestrator
    {
        private readonly DataStoreFactory _storeFactory = new();
        private readonly IAccountDataStore _accountStore;
        private readonly ITransactionDataStore _transactionStore;

        private readonly TransferDetector _transferDetector;
        private readonly RecurringDetector _recurringDetector;
        private readonly ForecastGenerator _forecastGenerator;
        private readonly FutureReconciler _futureReconciler;

        private const int RecurrenceMonths = 13;
        private const int TransferLookbackDays = 3;
        private const int InactiveGraceDays = 30;

        public TransactionUpdaterOrchestrator()
        {
            _transactionStore = _storeFactory.GetTransactionStore();
            _accountStore = _storeFactory.GetAccountStore();

            _transferDetector = new TransferDetector();
            _recurringDetector = new RecurringDetector();
            _forecastGenerator = new ForecastGenerator();
            _futureReconciler = new FutureReconciler();
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

                // Active if latest posted is within grace period of latest transfer
                if (latestPosted.Value >= latestTransfer.Value.AddDays(-InactiveGraceDays))
                {
                    activeAccounts.Add((account.Id, latestTransfer.Value, latestPosted.Value));
                }
            }

            // If no active accounts, skip transfer detection entirely
            if (activeAccounts.Any())
            {
                var earliestLatestTransfer = activeAccounts.Min(a => a.LatestTransfer);
                var transferStart = earliestLatestTransfer.AddDays(-TransferLookbackDays);
                var transferEnd = activeAccounts.Max(a => a.LatestPosted);

                // ------------------------------------------------------------
                // 2. Load posted transactions across ALL accounts for transfer detection
                // ------------------------------------------------------------
                var recentPosted = _transactionStore.GetPostedTransactions(transferStart, transferEnd).ToList();

                // ------------------------------------------------------------
                // 3. Detect transfers globally
                // ------------------------------------------------------------
                _transferDetector.DetectTransfers(recentPosted);

                foreach (var t in _transferDetector.Detected)
                    _transactionStore.AddPostedTransferTransaction(t);

                foreach (var m in _transferDetector.Matched)
                    _transactionStore.DeletePostedTransaction(m.Id);

                result.TransfersDetected = _transferDetector.Detected.Count;
            }

            // ------------------------------------------------------------
            // 4. Per-account recurrence detection, forecasting, reconciliation
            // ------------------------------------------------------------
            foreach (var account in accounts)
            {
                var latestPostedDate = _transactionStore.GetLatestPostedTransactionDate(account.Id);
                if (latestPostedDate is null)
                    continue;

                var recurrenceStart = latestPostedDate.Value.AddMonths(-RecurrenceMonths);

                // Load windowed ledger
                var posted = _transactionStore.GetPostedTransactions(recurrenceStart, latestPostedDate.Value).ToList();
                var postedTransfers = _transactionStore.GetPostedTransferTransactions(recurrenceStart, latestPostedDate.Value).ToList();

                var existingSingleRules = _transactionStore.GetRecurringSingleRules(account.Id).ToList();
                var existingTransferRules = _transactionStore.GetRecurringTransferRules(account.Id).ToList();

                // Detect recurring rules
                var newSingleRules = _recurringDetector.DetectRecurringSingles(posted, existingSingleRules);
                var newTransferRules = _recurringDetector.DetectRecurringTransfers(postedTransfers, existingTransferRules);

                result.RecurringRulesUpdated += newSingleRules.Count + newTransferRules.Count;

                // Merge rules
                var mergedSingleRules = existingSingleRules.Concat(newSingleRules).ToList();
                var mergedTransferRules = existingTransferRules.Concat(newTransferRules).ToList();

                // Forecast future transactions
                var futureSingles = _forecastGenerator.GenerateFutureSingles(mergedSingleRules);
                var futureTransfers = _forecastGenerator.GenerateFutureTransfers(mergedTransferRules);

                foreach (var f in futureSingles)
                    _transactionStore.AddFutureSingleTransaction(f);

                foreach (var f in futureTransfers)
                    _transactionStore.AddFutureTransferTransaction(f);

                result.ForecastUpdated = true;

                // Persist updated rules (NextOccurrence updated)
                foreach (var r in mergedSingleRules)
                    _transactionStore.UpdateRecurringSingleRule(r);

                foreach (var r in mergedTransferRules)
                    _transactionStore.UpdateRecurringTransferRule(r);

                // Reconcile future transactions
                var allPostedNow = _transactionStore.GetPostedTransactions(account.Id).ToList();
                var allPostedTransfersNow = _transactionStore.GetPostedTransferTransactions(account.Id).ToList();

                var allFutureSingles = _transactionStore.GetFutureSingleTransactions(account.Id).ToList();
                var allFutureTransfers = _transactionStore.GetFutureTransferTransactions(account.Id).ToList();

                _futureReconciler.ReconcileSingles(allPostedNow, allFutureSingles);
                _futureReconciler.ReconcileTransfers(allPostedTransfersNow, allFutureTransfers);

                foreach (var f in _futureReconciler.MatchedSingles)
                    _transactionStore.UpdateFutureSingleTransaction(f);

                foreach (var f in _futureReconciler.MatchedTransfers)
                    _transactionStore.UpdateFutureTransferTransaction(f);

                // Roll-off realized future items
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
