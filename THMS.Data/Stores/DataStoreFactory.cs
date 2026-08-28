using THMS.Configuration;
using THMS.Data.Stores.SQLite;

namespace THMS.Data.Stores
{
    public class DataStoreFactory
    {
        private static IAccountDataStore? _accountStore;
        private static IEnergyDataStore? _energyStore;
        private static IFinanceDataStore? _financeStore;
        private static ITransactionDataStore? _transactionStore;
        private static IVehicleDataStore? _vehicleStore;

        public IAccountDataStore GetAccountStore()
        {
            return _accountStore ??= CreateAccountStore();
        }

        public IEnergyDataStore GetEnergyStore()
        {
            return _energyStore ??= CreateEnergyStore();
        }

        public IFinanceDataStore GetFinanceStore()
        {
            return _financeStore ??= CreateFinanceStore();
        }

        public ITransactionDataStore GetTransactionStore()
        {
            return _transactionStore ??= CreateTransactionStore();
        }

        public IVehicleDataStore GetVehicleStore()
        {
            return _vehicleStore ??= CreateVehicleStore();
        }

        private IAccountDataStore CreateAccountStore()
        {
            return "Production" == AppConfig.Instance.Environment 
                ? new SQLiteAccountDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryAccountDataStore();
        }

        private IEnergyDataStore CreateEnergyStore()
        {
            return "Production" == AppConfig.Instance.Environment 
                ? new SQLiteEnergyDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryEnergyDataStore();
        }

        private IFinanceDataStore CreateFinanceStore()
        {
            return "Production" == AppConfig.Instance.Environment 
                ? new SQLiteFinanceDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryFinanceDataStore();
        }

        private ITransactionDataStore CreateTransactionStore()
        {
            return "Production" == AppConfig.Instance.Environment 
                ? new SQLiteTransactionDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryTransactionDataStore();
        }

        private IVehicleDataStore CreateVehicleStore()
        {
            return "Production" == AppConfig.Instance.Environment 
                ? new SQLiteVehicleDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryVehicleDataStore();
        }
    }
}
