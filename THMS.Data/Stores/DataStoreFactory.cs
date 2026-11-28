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
        private static IAccountStatementDataStore? _statementStore;
        private static IInvestmentBookStore? _investmentBookStore;
        private static IAssetDataStore? _assetStore;
        private static IIncomeDataStore? _incomeStore;
        private static IRetirement401kLimitStore? _retirement401kLimitStore;
        private static IRetirement401kMatchStore? _retirement401kMatchStore;
        private static IRetirement401kBirthYearStore? _retirement401kBirthYearStore;
        private static IHouseholdPersonStore? _householdPersonStore;

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

        public ICategoryDataStore GetCategoryStore()
        {
            var transactions = GetTransactionStore();
            if (transactions is ICategoryDataStore categories)
                return categories;

            throw new InvalidOperationException("Transaction store does not implement ICategoryDataStore.");
        }

        public IVehicleDataStore GetVehicleStore()
        {
            return _vehicleStore ??= CreateVehicleStore();
        }

        public IInvestmentBookStore GetInvestmentBookStore()
        {
            return _investmentBookStore ??= CreateInvestmentBookStore();
        }

        public IAccountStatementDataStore GetAccountStatementStore()
        {
            return _statementStore ??= CreateAccountStatementStore();
        }

        public IAssetDataStore GetAssetStore()
        {
            return _assetStore ??= CreateAssetStore();
        }

        public IIncomeDataStore GetIncomeStore()
        {
            return _incomeStore ??= CreateIncomeStore();
        }

        public IRetirement401kLimitStore GetRetirement401kLimitStore()
        {
            return _retirement401kLimitStore ??= CreateRetirement401kLimitStore();
        }

        public IRetirement401kMatchStore GetRetirement401kMatchStore()
        {
            return _retirement401kMatchStore ??= CreateRetirement401kMatchStore();
        }

        public IRetirement401kBirthYearStore GetRetirement401kBirthYearStore()
        {
            return _retirement401kBirthYearStore ??= CreateRetirement401kBirthYearStore();
        }

        public IHouseholdPersonStore GetHouseholdPersonStore()
        {
            return _householdPersonStore ??= CreateHouseholdPersonStore();
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

        private IInvestmentBookStore CreateInvestmentBookStore()
        {
            return "Production" == AppConfig.Instance.Environment
                ? new SQLiteInvestmentBookStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryInvestmentBookStore();
        }

        private IAccountStatementDataStore CreateAccountStatementStore()
        {
            return "Production" == AppConfig.Instance.Environment
                ? new SQLiteAccountStatementDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryAccountStatementDataStore();
        }

        private IAssetDataStore CreateAssetStore()
        {
            return "Production" == AppConfig.Instance.Environment
                ? new SQLiteAssetDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryAssetDataStore();
        }

        private IIncomeDataStore CreateIncomeStore()
        {
            return "Production" == AppConfig.Instance.Environment
                ? new SQLiteIncomeDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryIncomeDataStore();
        }

        private IRetirement401kLimitStore CreateRetirement401kLimitStore()
        {
            return "Production" == AppConfig.Instance.Environment
                ? new SQLiteRetirement401kLimitStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryRetirement401kLimitStore();
        }

        private IRetirement401kMatchStore CreateRetirement401kMatchStore()
        {
            return "Production" == AppConfig.Instance.Environment
                ? new SQLiteRetirement401kMatchStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryRetirement401kMatchStore();
        }

        private IRetirement401kBirthYearStore CreateRetirement401kBirthYearStore()
        {
            return "Production" == AppConfig.Instance.Environment
                ? new SQLiteRetirement401kBirthYearStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryRetirement401kBirthYearStore();
        }

        private IHouseholdPersonStore CreateHouseholdPersonStore()
        {
            return "Production" == AppConfig.Instance.Environment
                ? new SQLiteHouseholdPersonStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryHouseholdPersonStore();
        }

        private IVehicleDataStore CreateVehicleStore()
        {
            return "Production" == AppConfig.Instance.Environment 
                ? new SQLiteVehicleDataStore(AppConfig.Instance.SQLiteDataBase)
                : new InMemoryVehicleDataStore();
        }
    }
}
