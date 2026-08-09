using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public class InMemoryVehicleDataStore : IVehicleDataStore
    {
        private readonly InMemoryVehicleStore _vehicleStore = new();
        private readonly InMemoryMileageRecordStore _mileageStore = new();
        private readonly InMemoryIceMileageStore _iceMileageStore;
        private readonly InMemoryEvChargeSessionStore _evChargeSessionStore;
        private readonly InMemoryMaintenanceInvoiceStore _maintenanceStore = new();

        public InMemoryVehicleDataStore()
        {
            _iceMileageStore = new InMemoryIceMileageStore(_mileageStore);
            _evChargeSessionStore = new InMemoryEvChargeSessionStore(_mileageStore);

            _vehicleStore.Upsert(new VehicleEv
            {
                Id = Guid.NewGuid(),
                Make = "Ford",
                Model = "Mustang Mach-E",
                Year = 2023,
                Vin = "3FMTK3R74PMA89745",
                BatteryCapacityKwh = 92,
                Name = "Tim's",
            });

            _vehicleStore.Upsert(new VehicleIce
            {
                Id = Guid.NewGuid(),
                Make = "Ford",
                Model = "Escape SEL",
                Year = 2018,
                Vin = "1FMCU9HD5JUA71357",
                Name = "Julie's",
            });
        }

        // ---------------------------------------------------------
        // VEHICLES
        // ---------------------------------------------------------

        public void UpsertVehicle(VehicleBase vehicle) => _vehicleStore.Upsert(vehicle);

        public VehicleBase? GetVehicle(Guid id) => _vehicleStore.Get(id);

        public IEnumerable<VehicleBase> GetAllVehicles() => _vehicleStore.GetAll();

        // ---------------------------------------------------------
        // ICE MILEAGE
        // ---------------------------------------------------------

        public void UpsertIceMileageRecord(IceMileageRecord record) => _iceMileageStore.Upsert(record);

        public IceMileageRecord? GetEarliestIceMileageRecord(Guid vehicleId) =>
            _iceMileageStore.GetEarliest(vehicleId);

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end) =>
            _iceMileageStore.GetRange(vehicleId, start, end);

        // ---------------------------------------------------------
        // SHARED MILEAGE
        // ---------------------------------------------------------

        public decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end) =>
            _mileageStore.GetMilesDrivenInPeriod(vehicleId, start, end);

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS
        // ---------------------------------------------------------

        public void UpsertEvChargeSession(EvChargeSession session) => _evChargeSessionStore.Upsert(session);

        public EvChargeSession? GetEvChargeSession(Guid sessionId) => _evChargeSessionStore.Get(sessionId);

        public IEnumerable<EvChargeSession> GetEvChargeSessions(Guid vehicleId, DateTime start, DateTime end) =>
            _evChargeSessionStore.GetRange(vehicleId, start, end);

        public EvChargeSession? GetLatestEvChargeSession() =>
            _evChargeSessionStore.GetLatest();

        public void DeleteEvChargeSession(Guid sessionId) => _evChargeSessionStore.Delete(sessionId);

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------

        public void UpsertMaintenanceInvoice(MaintenanceInvoiceRecord invoice) =>
            _maintenanceStore.Upsert(invoice);

        public IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end) =>
            _maintenanceStore.GetRange(vehicleId, start, end);

        public decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end) =>
            _maintenanceStore.GetTotalCost(vehicleId, start, end);
    }
}
