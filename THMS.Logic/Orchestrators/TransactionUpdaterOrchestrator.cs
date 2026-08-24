using THMS.Data.Stores;
using THMS.Domain.Finance;

namespace THMS.Logic.Orchestrators
{
    public class TransactionUpdaterOrchestrator
    {
        private readonly ITransactionDataStore _transactionStore;
        private readonly IAccountDataStore _accountStore;

        public TransactionUpdaterOrchestrator(
            ITransactionDataStore transactionStore,
            IAccountDataStore accountStore)
        {
            _transactionStore = transactionStore;
            _accountStore = accountStore;
        }

        public UpdaterResult RunFullUpdate()
        {
            // TODO: integrate PlaidUpdaterService
            // TODO: detect transfers
            // TODO: detect recurring
            // TODO: categorize
            // TODO: forecast
            // TODO: roll-off

            return new UpdaterResult
            {
                AccountsUpdated = 0,
                TransactionsImported = 0,
                TransfersDetected = 0,
                RecurringRulesUpdated = 0,
                ForecastUpdated = false,
                RollOffCompleted = false
            };
        }
    }
}
