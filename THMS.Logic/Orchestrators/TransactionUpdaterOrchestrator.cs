using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class TransactionUpdaterOrchestrator
    {
        private readonly ITransactionDataStore _transactionStore;
        private readonly IAccountDataStore _accountStore;

        private readonly ExternalTransactionAccess _accessor;
        private readonly TransferDetector _transferDetector;
        private readonly RecurringDetector _recurringDetector;
        private readonly Categorizer _categorizer;
        private readonly ForecastGenerator _forecastGenerator;
        private readonly RollOffEngine _rollOffEngine;

        public TransactionUpdaterOrchestrator(
            ITransactionDataStore transactionStore,
            IAccountDataStore accountStore)
        {
            _transactionStore = transactionStore;
            _accountStore = accountStore;

            _transferDetector = new TransferDetector();
            _recurringDetector = new RecurringDetector();
            _categorizer = new Categorizer();
            _forecastGenerator = new ForecastGenerator();
            _rollOffEngine = new RollOffEngine();
            _accessor = new ExternalTransactionAccess();
        }

        public UpdaterResult RunFullUpdate()
        {
            var result = new UpdaterResult();

            // 1. Load accounts
            var accounts = _accountStore.GetAllAccounts().ToList();

            // 2. Fetch posted transactions
            var posted = FetchAllPosted(accounts);
            result.TransactionsImported = posted.Count;

            // 3. Normalize posted
            NormalizePosted(posted);

            // 4. Save posted
            _transactionStore.SavePostedTransactions(posted);

            // 5. Detect transfers
            var transfers = _transferDetector.DetectTransfers(posted);
            _transactionStore.SavePostedTransfers(transfers);
            result.TransfersDetected = transfers.Count;

            // 6. Detect recurring rules
            var recurringSingles = _recurringDetector.DetectRecurringSingles(posted);
            var recurringTransfers = _recurringDetector.DetectRecurringTransfers(posted);
            _transactionStore.SaveRecurringRules(recurringSingles, recurringTransfers);
            result.RecurringRulesUpdated = recurringSingles.Count + recurringTransfers.Count;

            // 7. Categorize posted
            _categorizer.ApplyCategories(posted);
            _transactionStore.UpdateCategories(posted);

            // 8. Forecast future transactions
            var futureSingles = _forecastGenerator.GenerateFutureSingles(recurringSingles);
            var futureTransfers = _forecastGenerator.GenerateFutureTransfers(recurringTransfers);
            _transactionStore.SaveFutureTransactions(futureSingles, futureTransfers);
            result.ForecastUpdated = true;

            // 9. Roll-off realized future items
            var rollOffDone = _rollOffEngine.RollOffRealized(
                posted,
                futureSingles,
                futureTransfers);

            result.RollOffCompleted = rollOffDone;

            // 10. Accounts updated count
            result.AccountsUpdated = accounts.Count;

            return result;
        }

        private List<PostedTransaction> FetchAllPosted(List<Account> accounts)
        {
            var posted = new List<PostedTransaction>();

            foreach (var acct in accounts)
            {
                var acctPosted = _accessor.FetchPostedTransactions(acct);
                posted.AddRange(acctPosted);
            }

            return posted;
        }

        private void NormalizePosted(List<PostedTransaction> posted)
        {
            foreach (var p in posted)
            {
                p.Description = p.Description?.Trim() ?? "";
                p.Amount = Math.Round(p.Amount, 2);
                p.Date = p.Date.Date;
            }
        }
    }
}
