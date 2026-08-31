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
        private readonly DataStoreFactory _storeFactory = new();
        private readonly IAccountDataStore _accountStore;
        private readonly ITransactionDataStore _transactionStore;

        private readonly ExternalTransactionAccess _accessor;
        private readonly TransferDetector _transferDetector;
        private readonly RecurringDetector _recurringDetector;
        private readonly Categorizer _categorizer;
        private readonly ForecastGenerator _forecastGenerator;

        public TransactionUpdaterOrchestrator()
        {
            _transactionStore = _storeFactory.GetTransactionStore(); ;
            _accountStore = _storeFactory.GetAccountStore(); ;

            _transferDetector = new TransferDetector();
            _recurringDetector = new RecurringDetector();
            _categorizer = new Categorizer();
            _forecastGenerator = new ForecastGenerator();
            _accessor = new ExternalTransactionAccess();
        }

        public async Task<UpdaterResult> RunFullUpdateAsync()
        {
            var result = new UpdaterResult();

            // 1. Load accounts
            var accounts = _accountStore.GetAllAccounts().ToList();

            // 2. Fetch posted transactions
            var posted = await FetchAllPostedAsync(accounts);
            result.TransactionsImported = posted.Count;

            // 3. Normalize posted
            NormalizePosted(posted);

            // 4. Categorize posted
            _categorizer.ApplyCategories(posted);
            foreach (var item in posted)
            {
                _transactionStore.UpdatePostedTransaction(item);
            }

            // 5. Save posted
            foreach (var item in posted)
            {
                _transactionStore.AddPostedTransaction(item);
            }

            // 6. Detect transfers
            _transferDetector.DetectTransfers(posted);
            foreach (var item in _transferDetector.Detected)
            {
                _transactionStore.AddPostedTransferTransaction(item);
            }
            foreach (var item in _transferDetector.Matched)
            {
                _transactionStore.DeletePostedTransaction(item.Id);
            }
            result.TransfersDetected = _transferDetector.Detected.Count;

            // 7. Detect recurring rules
            var recurringSingles = _recurringDetector.DetectRecurringSingles(posted);
            foreach (var item in recurringSingles)
            {
                _transactionStore.AddRecurringSingleRule(item);
            }
            var recurringTransfers = _recurringDetector.DetectRecurringTransfers(posted);
            foreach (var item in recurringTransfers)
            {
                _transactionStore.AddRecurringTransferRule(item);
            }
            result.RecurringRulesUpdated = recurringSingles.Count + recurringTransfers.Count;

            // 8. Forecast future transactions
            var futureSingles = _forecastGenerator.GenerateFutureSingles(recurringSingles);
            foreach (var item in futureSingles) 
            {
                _transactionStore.AddFutureSingleTransaction(item);
            }
            var futureTransfers = _forecastGenerator.GenerateFutureTransfers(recurringTransfers);
            foreach (var item in futureTransfers)
            {
                _transactionStore.AddFutureTransferTransaction(item);
            }
            result.ForecastUpdated = true;

            // 9. Roll-off realized future items

            result.RollOffCompleted = rollOffDone;

            // 10. Accounts updated count
            result.AccountsUpdated = accounts.Count;

            return result;
        }

        private async Task<List<PostedTransaction>> FetchAllPostedAsync(List<Account> accounts)
        {
            var posted = new List<PostedTransaction>();

            foreach (var acct in accounts)
            {
                var acctPosted = await _accessor.FetchPostedTransactionsAsync(acct, DateTime.Today.AddMonths(-3), DateTime.Today);
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
