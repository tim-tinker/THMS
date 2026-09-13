using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Recurrence;
using THMS.Logic.Finance.Transfer;

namespace THMS.Logic.Orchestrators
{
    public class TransactionUpdaterOrchestrator
    {
        private readonly IAccountDataStore _accountStore;
        private readonly ITransactionDataStore _transactionStore;
        private readonly IAccountStatementDataStore _statements;

        private readonly TransferDetector _transferDetector = new();
        private readonly RecurringDetector _recurringDetector = new();
        private readonly FutureReconciler _futureReconciler = new();
        private readonly BudgetOrchestrator _budgetOrchestrator;

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
            : this(accountStore, transactionStore, new DataStoreFactory().GetAccountStatementStore())
        {
        }

        public TransactionUpdaterOrchestrator(
            IAccountDataStore accountStore,
            ITransactionDataStore transactionStore,
            IAccountStatementDataStore statements)
        {
            _accountStore = accountStore;
            _transactionStore = transactionStore;
            _statements = statements;
            _budgetOrchestrator = new BudgetOrchestrator(transactionStore);
        }

        public UpdaterResult RunLedgerUpdate()
        {
            var result = new UpdaterResult();

            var accounts = _accountStore.GetAllAccounts().ToList();
            result.AccountsUpdated = accounts.Count;

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

            foreach (var account in accounts)
            {
                var latestPostedDate = _transactionStore.GetLatestPostedTransactionDate(account.Id);
                if (latestPostedDate is not null)
                {
                    var recurrenceStart = latestPostedDate.Value.AddMonths(-RecurrenceMonths);

                    var posted = _transactionStore.GetPostedTransactions(account.Id)
                        .Where(t => t.Date >= recurrenceStart && t.Date <= latestPostedDate.Value)
                        .ToList();
                    var postedTransfers = _transactionStore.GetPostedTransferTransactions(account.Id)
                        .Where(t => t.Date >= recurrenceStart && t.Date <= latestPostedDate.Value)
                        .ToList();

                    foreach (var duplicateId in RecurringDetector.DuplicateAutoRuleIds(
                        _transactionStore.GetRecurringSingleRules(account.Id)))
                        _transactionStore.DeleteRecurringSingleRule(duplicateId);

                    foreach (var duplicateId in RecurringDetector.DuplicateAutoRuleIds(
                        _transactionStore.GetRecurringTransferRules(account.Id)))
                        _transactionStore.DeleteRecurringTransferRule(duplicateId);

                    var existingSingleRules = _transactionStore.GetRecurringSingleRules(account.Id).ToList();
                    var existingTransferRules = _transactionStore.GetRecurringTransferRules(account.Id).ToList();

                    var newSingleRules = _recurringDetector.DetectRecurringSingles(posted, existingSingleRules);
                    var newTransferRules = _recurringDetector.DetectRecurringTransfers(postedTransfers, existingTransferRules);

                    result.RecurringRulesUpdated += newSingleRules.Count + newTransferRules.Count;

                    foreach (var r in newSingleRules)
                        _transactionStore.AddRecurringSingleRule(r);

                    foreach (var r in existingSingleRules.Where(r => !r.IsUserCreated))
                        _transactionStore.UpdateRecurringSingleRule(r);

                    foreach (var r in newTransferRules)
                        _transactionStore.AddRecurringTransferRule(r);

                    foreach (var r in existingTransferRules.Where(r => !r.IsUserCreated))
                        _transactionStore.UpdateRecurringTransferRule(r);

                    var allPostedNow = _transactionStore.GetPostedTransactions(account.Id).ToList();
                    var allPostedTransfersNow = _transactionStore.GetPostedTransferTransactions(account.Id).ToList();
                    var allSingleRules = existingSingleRules.Concat(newSingleRules).ToList();
                    var allTransferRules = existingTransferRules.Concat(newTransferRules).ToList();

                    _futureReconciler.ReconcileSingles(allPostedNow, allSingleRules, dayTolerance: 4);
                    _futureReconciler.ReconcileTransfers(allPostedTransfersNow, allTransferRules, dayTolerance: 4);

                    foreach (var r in _futureReconciler.MatchedSingleRules)
                        _transactionStore.UpdateRecurringSingleRule(r);

                    foreach (var r in _futureReconciler.MatchedTransferRules)
                        _transactionStore.UpdateRecurringTransferRule(r);

                    foreach (var f in _transactionStore.GetFutureSingleTransactions(account.Id).Where(f => !f.IsUserCreated))
                        _transactionStore.DeleteFutureSingleTransaction(f.Id);

                    foreach (var f in _transactionStore.GetFutureTransferTransactions(account.Id).Where(f => !f.IsUserCreated))
                        _transactionStore.DeleteFutureTransferTransaction(f.Id);

                    result.ForecastUpdated = true;
                    result.RollOffCompleted = true;
                }

                RefreshPostedBalance(account);
            }

            foreach (var account in accounts)
            {
                _budgetOrchestrator.EnsureSuggestedRules(account.Id);
                _budgetOrchestrator.RefreshAccount(account.Id);
            }

            return result;
        }

        private void RefreshPostedBalance(Account account)
        {
            var statements = _statements.GetForAccount(account.Id);
            if (!PostedBalanceCalculator.TryResolveAnchor(account, statements, out var anchor))
                return;

            ApplyAnchor(account, anchor);
        }

        private void ApplyAnchor(Account account, PostedBalanceAnchor anchor)
        {
            var activity = _transactionStore.SumPostedAmountsAfter(account.Id, anchor.AsOf)
                + _transactionStore.SumPostedTransferAmountsAfter(account.Id, anchor.AsOf);
            var balance = PostedBalanceCalculator.ComputeFromAnchor(anchor, activity);
            PostedBalanceCalculator.ApplyLedgerBalance(account, balance);
            account.BalanceAsOf = DateTime.Today;
            _accountStore.UpsertAccount(account);
        }
    }
}
