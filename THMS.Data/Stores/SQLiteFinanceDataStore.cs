using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqliteStores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteFinanceDataStore : IFinanceDataStore
    {
        private readonly string _connectionString;

        private readonly SqliteElectricUtilityBillStore _utilityBills = new();
        private readonly SqliteElectricContractStore _electricContracts = new();
        private readonly SqliteGasPurchaseStore _gasPurchases = new();

        public SQLiteFinanceDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = OpenConnection();
            InitializeSchema(conn);
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void InitializeSchema(SqliteConnection conn)
        {
            _utilityBills.InitializeSchema(conn);
            _electricContracts.InitializeSchema(conn);
            _gasPurchases.InitializeSchema(conn);
        }

        // ---------------------------------------------------------
        // ELECTRIC UTILITY BILLS
        // ---------------------------------------------------------

        public void UpsertElectricUtilityBill(ElectricUtilityBill bill)
        {
            using var conn = OpenConnection();
            _utilityBills.Upsert(conn, bill);
        }

        public ElectricUtilityBill? GetElectricUtilityBill(Guid billId)
        {
            using var conn = OpenConnection();
            return _utilityBills.Get(conn, billId);
        }

        public ElectricUtilityBill? GetElectricUtilityBillForDate(DateTime date)
        {
            using var conn = OpenConnection();
            return _utilityBills.GetForDate(conn, date);
        }

        public IEnumerable<ElectricUtilityBill> GetElectricUtilityBills(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _utilityBills.GetRange(conn, start, end).ToList();
        }

        public ElectricUtilityBill? GetLatestElectricUtilityBill()
        {
            using var conn = OpenConnection();
            return _utilityBills.GetLatest(conn);
        }

        // ---------------------------------------------------------
        // GAS PURCHASES
        // ---------------------------------------------------------

        public void UpsertGasPurchase(GasPurchase purchase)
        {
            using var conn = OpenConnection();
            _gasPurchases.Upsert(conn, purchase);
        }

        public IEnumerable<GasPurchase> GetGasPurchases(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _gasPurchases.GetRange(conn, vehicleId, start, end).ToList();
        }

        // ---------------------------------------------------------
        // ELECTRIC CONTRACTS
        // ---------------------------------------------------------

        public void UpsertElectricContract(ElectricContract contract)
        {
            using var conn = OpenConnection();
            _electricContracts.Upsert(conn, contract);
        }

        public ElectricContract? GetElectricContract(Guid contractId)
        {
            using var conn = OpenConnection();
            return _electricContracts.Get(conn, contractId);
        }

        public ElectricContract? GetElectricContractForDate(DateTime date)
        {
            using var conn = OpenConnection();
            return _electricContracts.GetForDate(conn, date);
        }

        public IEnumerable<ElectricContract> GetElectricContracts(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _electricContracts.GetRange(conn, start, end).ToList();
        }

        public ElectricContract? GetLatestElectricContract()
        {
            using var conn = OpenConnection();
            return _electricContracts.GetLatest(conn);
        }
    }
}
