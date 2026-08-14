using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqliteStores;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteVehicleDataStore : IVehicleDataStore
    {
        private readonly string _connectionString;

        private readonly SqliteMileageRecordStore _mileageStore = new();
        private readonly SqliteVehicleStore _vehicleStore = new();
        private readonly SqliteIceMileageStore _iceMileageStore;
        private readonly SqliteEvChargeSessionStore _evChargeSessionStore;
        private readonly SqliteMaintenanceInvoiceStore _maintenanceStore = new();

        public SQLiteVehicleDataStore(string connectionString)
        {
            _connectionString = connectionString;
            _iceMileageStore = new SqliteIceMileageStore(_mileageStore);
            _evChargeSessionStore = new SqliteEvChargeSessionStore(_mileageStore);

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
            _vehicleStore.InitializeSchema(conn);
            _mileageStore.InitializeSchema(conn);
            _iceMileageStore.InitializeSchema(conn);
            _evChargeSessionStore.InitializeSchema(conn);
            _maintenanceStore.InitializeSchema(conn);
        }

        // ---------------------------------------------------------
        // VEHICLES
        // ---------------------------------------------------------

        public void UpsertVehicle(VehicleBase vehicle)
        {
            using var conn = OpenConnection();
            _vehicleStore.Upsert(conn, vehicle);
        }

        public VehicleBase? GetVehicle(Guid id)
        {
            using var conn = OpenConnection();
            return _vehicleStore.Get(conn, id);
        }

        public IEnumerable<VehicleBase> GetAllVehicles()
        {
            using var conn = OpenConnection();
            return _vehicleStore.GetAll(conn).ToList();
        }

        // ---------------------------------------------------------
        // ICE MILEAGE
        // ---------------------------------------------------------

        public void UpsertIceMileageRecord(IceMileageRecord record)
        {
            using var conn = OpenConnection();
            _iceMileageStore.Upsert(conn, record);
        }

        public IceMileageRecord? GetEarliestIceMileageRecord(Guid vehicleId)
        {
            using var conn = OpenConnection();
            return _iceMileageStore.GetEarliest(conn, vehicleId);
        }

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _iceMileageStore.GetRange(conn, vehicleId, start, end).ToList();
        }

        // ---------------------------------------------------------
        // SHARED MILEAGE
        // ---------------------------------------------------------

        public decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _mileageStore.GetMilesDrivenInPeriod(conn, vehicleId, start, end);
        }

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS
        // ---------------------------------------------------------

        public void UpsertEvChargeSession(EvChargeSession session)
        {
            using var conn = OpenConnection();
            _evChargeSessionStore.Upsert(conn, session);
        }

        public EvChargeSession? GetEvChargeSession(Guid sessionId)
        {
            using var conn = OpenConnection();
            return _evChargeSessionStore.Get(conn, sessionId);
        }

        public IEnumerable<EvChargeSession> GetEvChargeSessions(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _evChargeSessionStore.GetRange(conn, vehicleId, start, end).ToList();
        }

        public EvChargeSession? GetLatestEvChargeSession()
        {
            using var conn = OpenConnection();
            return _evChargeSessionStore.GetLatest(conn);
        }

        public EvChargeSession? GetLatestEvChargeSession(Guid vehicleId)
        {
            using var conn = OpenConnection();
            return _evChargeSessionStore.GetLatest(conn, vehicleId);
        }

        public void DeleteEvChargeSession(Guid sessionId)
        {
            using var conn = OpenConnection();
            _evChargeSessionStore.Delete(conn, sessionId);
        }

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------

        public void UpsertMaintenanceInvoice(MaintenanceInvoiceRecord invoice)
        {
            using var conn = OpenConnection();
            _maintenanceStore.Upsert(conn, invoice);
        }

        public IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _maintenanceStore.GetRange(conn, vehicleId, start, end).ToList();
        }

        public decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _maintenanceStore.GetTotalCost(conn, vehicleId, start, end);
        }
    }
}
