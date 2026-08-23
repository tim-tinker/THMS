using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.External.Plaid;

namespace THMS.Logic.Orchestrators
{
    public class RegisterUpdateOrchestrator
    {
        private readonly AccountSyncOrchestrator _accountSync;
        private readonly TransactionImportOrchestrator _importer;
        private readonly TransactionOrchestrator _txOrchestrator;

        public RegisterUpdateOrchestrator(
            IAccountDataStore accountStore,
            ITransactionDataStore txStore,
            PlaidServiceClient plaidClient)
        {
            // Build orchestrators
            _accountSync = new AccountSyncOrchestrator(plaidClient, accountStore);
            _importer = new TransactionImportOrchestrator(plaidClient, txStore);
            _txOrchestrator = new TransactionOrchestrator(txStore);
        }
        public async Task UpdateAccountAsync(Account account)
        {
            // 1. Sync account balances
            await _accountSync.SyncAsync(account);

            // 2. Import posted transactions
            await _importer.ImportAsync(account);

            // 3. Reconcile future singles
            _txOrchestrator.ReconcileFutureSingles(account.Id);

            // 4. Reconcile future transfers
            _txOrchestrator.ReconcileFutureTransfers(account.Id);

            // 5. Generate new future transactions
            _txOrchestrator.GenerateFutureTransactions(
                account.Id,
                forecastEnd: DateTime.UtcNow.AddMonths(3));

            // 6. Roll off realized future transactions
            _txOrchestrator.RollOffRealizedFutureTransactions(
                cutoff: DateTime.UtcNow.AddMonths(-1));
        }
    }
}
