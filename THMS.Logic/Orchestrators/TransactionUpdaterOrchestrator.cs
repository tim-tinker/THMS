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

            foreach (var account in accounts)
            {
                // ------------------------------------------------------------
                // 1. Load historical ledger
                // ------------------------------------------------------------
                var posted = _transactionStore.GetPostedTransactions(account.Id).ToList();
                var postedTransfers = _transactionStore.GetPostedTransferTransactions(account.Id).ToList();

                var existingSingleRules = _transactionStore.GetRecurringSingleRules(account.Id).ToList();
                var existingTransferRules = _transactionStore.GetRecurringTransferRules(account.Id).ToList();

                // ------------------------------------------------------------
                // 2. Detect transfers (historical ledger only)
                // ------------------------------------------------------------
                _transferDetector.DetectTransfers(posted);

                foreach (var t in _transferDetector.Detected)
                    _transactionStore.AddPostedTransferTransaction(t);

                foreach (var m in _transferDetector.Matched)
                    _transactionStore.DeletePostedTransaction(m.Id);

                result.TransfersDetected += _transferDetector.Detected.Count;

                // Reload transfers after detection
                postedTransfers = _transactionStore.GetPostedTransferTransactions(account.Id).ToList();

                // ------------------------------------------------------------
                // 3. Detect recurring rules (historical ledger only)
                // ------------------------------------------------------------
                var newSingleRules =
                    _recurringDetector.DetectRecurringSingles(
                        posted,
                        existingSingleRules);

                var newTransferRules =
                    _recurringDetector.DetectRecurringTransfers(
                        postedTransfers,
                        existingTransferRules);

                result.RecurringRulesUpdated += newSingleRules.Count + newTransferRules.Count;

                // ------------------------------------------------------------
                // 4. Merge existing + new rules
                // ------------------------------------------------------------
                var mergedSingleRules = existingSingleRules.Concat(newSingleRules).ToList();
                var mergedTransferRules = existingTransferRules.Concat(newTransferRules).ToList();

                // ------------------------------------------------------------
                // 5. Forecast future transactions (updates NextOccurrence)
                // ------------------------------------------------------------
                var futureSingles = _forecastGenerator.GenerateFutureSingles(mergedSingleRules);
                var futureTransfers = _forecastGenerator.GenerateFutureTransfers(mergedTransferRules);

                foreach (var f in futureSingles)
                    _transactionStore.AddFutureSingleTransaction(f);

                foreach (var f in futureTransfers)
                    _transactionStore.AddFutureTransferTransaction(f);

                result.ForecastUpdated = true;

                // ------------------------------------------------------------
                // 6. Upsert ALL recurring rules (NextOccurrence updated)
                // ------------------------------------------------------------
                foreach (var r in mergedSingleRules)
                    _transactionStore.UpdateRecurringSingleRule(r);

                foreach (var r in mergedTransferRules)
                    _transactionStore.UpdateRecurringTransferRule(r);

                // ------------------------------------------------------------
                // 7. Reconcile future transactions
                // ------------------------------------------------------------
                var allPosted = _transactionStore.GetPostedTransactions(account.Id).ToList();
                var allPostedTransfers = _transactionStore.GetPostedTransferTransactions(account.Id).ToList();

                var allFutureSingles = _transactionStore.GetFutureSingleTransactions(account.Id).ToList();
                var allFutureTransfers = _transactionStore.GetFutureTransferTransactions(account.Id).ToList();

                _futureReconciler.ReconcileSingles(allPosted, allFutureSingles);
                _futureReconciler.ReconcileTransfers(allPostedTransfers, allFutureTransfers);

                foreach (var f in _futureReconciler.MatchedSingles)
                    _transactionStore.UpdateFutureSingleTransaction(f);

                foreach (var f in _futureReconciler.MatchedTransfers)
                    _transactionStore.UpdateFutureTransferTransaction(f);

                // ------------------------------------------------------------
                // 8. Roll-off realized future items
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
