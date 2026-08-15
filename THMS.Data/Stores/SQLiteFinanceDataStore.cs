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
        private readonly SqliteGasPurchaseStore _gasPurchases = new();
        private readonly SqliteEvChargeSessionCostStore _evChargeSessionCosts = new();

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

        public IEnumerable<ElectricUtilityBill> GetElectricUtilityBills(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _utilityBills.GetRange(conn, start, end).ToList();
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
        // INCOMPLETE COST RECORDS
        // ---------------------------------------------------------

        public IEnumerable<BaseEvChargeSession> GetEvChargeSessionsWithMissingCost()
        {
            using var conn = OpenConnection();
            return _evChargeSessionCosts.GetWithMissingCost(conn).ToList();
        }

        public IEnumerable<GasPurchase> GetGasPurchasesWithMissingCost()
        {
            using var conn = OpenConnection();
            return _gasPurchases.GetWithMissingCost(conn).ToList();
        }

        // ---------------------------------------------------------
        // COST UPDATES
        // ---------------------------------------------------------

        public void UpdateEvChargeSessionCost(Guid sessionId, decimal cost)
        {
            using var conn = OpenConnection();
            _evChargeSessionCosts.UpdateCost(conn, sessionId, cost);
        }

        public void UpdateGasPurchaseCost(Guid purchaseId, decimal cost)
        {
            using var conn = OpenConnection();
            _gasPurchases.UpdateCost(conn, purchaseId, cost);
        }
    }
}
