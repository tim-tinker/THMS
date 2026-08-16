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
        private readonly SqliteBaseEvChargeSessionStore _baseEvStore;
        private readonly SqliteCommercialEvChargeSessionStore _commercialEvStore = new();
        private readonly SqliteHomeEvChargeSessionStore _homeEvStore = new();
        private readonly SqliteHomeEvChargeAttributionStore _attribStore = new();
        private readonly SqliteHomeEvChargeBillingStore _billingStore = new();
        private readonly SqliteMaintenanceInvoiceStore _maintenanceStore = new();

        public SQLiteVehicleDataStore(string connectionString)
        {
            _connectionString = connectionString;
            _iceMileageStore = new SqliteIceMileageStore(_mileageStore);
            _baseEvStore = new SqliteBaseEvChargeSessionStore(_mileageStore);

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
            _baseEvStore.InitializeSchema(conn);
            _commercialEvStore.InitializeSchema(conn);
            _homeEvStore.InitializeSchema(conn);
            _attribStore.InitializeSchema(conn);
            _billingStore.InitializeSchema(conn);
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
        // BASE EV CHARGE SESSIONS
        // ---------------------------------------------------------

        public void UpsertBaseEvChargeSession(BaseEvChargeSession session)
        {
            using var conn = OpenConnection();
            _baseEvStore.Upsert(conn, session);
        }

        public BaseEvChargeSession? GetBaseEvChargeSession(Guid sessionId)
        {
            using var conn = OpenConnection();
            return _baseEvStore.Get(conn, sessionId);
        }

        public IEnumerable<BaseEvChargeSession> GetBaseEvChargeSessions(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _baseEvStore.GetRange(conn, vehicleId, start, end).ToList();
        }

        public BaseEvChargeSession? GetLatestBaseEvChargeSession()
        {
            using var conn = OpenConnection();
            return _baseEvStore.GetLatest(conn);
        }

        public BaseEvChargeSession? GetLatestBaseEvChargeSession(Guid vehicleId)
        {
            using var conn = OpenConnection();
            return _baseEvStore.GetLatest(conn, vehicleId);
        }

        public void DeleteBaseEvChargeSession(Guid sessionId)
        {
            using var conn = OpenConnection();
            _commercialEvStore.Delete(conn, sessionId);
            _attribStore.Delete(conn, sessionId);
            _billingStore.Delete(conn, sessionId);
            _homeEvStore.Delete(conn, sessionId);
            _baseEvStore.Delete(conn, sessionId);
        }

        // ---------------------------------------------------------
        // COMMERCIAL EV CHARGE SESSIONS
        // ---------------------------------------------------------

        public void UpsertCommercialEvChargeSession(CommercialEvChargeSession session)
        {
            using var conn = OpenConnection();
            _commercialEvStore.Upsert(conn, session);
        }

        public CommercialEvChargeSession? GetCommercialEvChargeSession(Guid sessionId)
        {
            using var conn = OpenConnection();
            var commercial = _commercialEvStore.Get(conn, sessionId);
            if (commercial == null)
                return null;

            ApplyBaseFields(commercial, _baseEvStore.Get(conn, sessionId));
            return commercial;
        }

        // ---------------------------------------------------------
        // HOME EV CHARGE SESSIONS
        // ---------------------------------------------------------

        public void UpsertHomeEvChargeSession(HomeEvChargeSession session)
        {
            using var conn = OpenConnection();
            _homeEvStore.Upsert(conn, session);
        }

        public HomeEvChargeSession? GetHomeEvChargeSession(Guid sessionId)
        {
            using var conn = OpenConnection();
            var home = _homeEvStore.Get(conn, sessionId);
            if (home == null)
                return null;

            ApplyBaseFields(home, _baseEvStore.Get(conn, sessionId));
            home.Attribution = _attribStore.Get(conn, sessionId);
            home.Billing = _billingStore.Get(conn, sessionId);
            return home;
        }

        // ---------------------------------------------------------
        // HOME EV CHARGE ATTRIBUTION
        // ---------------------------------------------------------

        public void UpsertHomeEvChargeAttribution(Guid sessionId, HomeEvChargeAttribution attribution)
        {
            using var conn = OpenConnection();
            _attribStore.Upsert(conn, sessionId, attribution);
        }

        public HomeEvChargeAttribution? GetHomeEvChargeAttribution(Guid sessionId)
        {
            using var conn = OpenConnection();
            return _attribStore.Get(conn, sessionId);
        }

        // ---------------------------------------------------------
        // HOME EV CHARGE BILLING
        // ---------------------------------------------------------

        public void UpsertHomeEvChargeBilling(Guid sessionId, HomeEvChargeBilling billing)
        {
            using var conn = OpenConnection();
            _billingStore.Upsert(conn, sessionId, billing);
        }

        public HomeEvChargeBilling? GetHomeEvChargeBilling(Guid sessionId)
        {
            using var conn = OpenConnection();
            return _billingStore.Get(conn, sessionId);
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

        private static void ApplyBaseFields(BaseEvChargeSession target, BaseEvChargeSession? source)
        {
            if (source == null)
                return;

            target.VehicleId = source.VehicleId;
            target.VehicleName = source.VehicleName;
            target.EndTime = source.EndTime;
            target.OdometerMiles = source.OdometerMiles;
            target.LastOdometer = source.LastOdometer;
            target.LastSoc = source.LastSoc;
            target.StartTime = source.StartTime;
            target.StartSoc = source.StartSoc;
            target.EndSoc = source.EndSoc;
            target.KwhAdded = source.KwhAdded;
        }
    }
}
