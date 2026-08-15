using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public class InMemoryVehicleDataStore : IVehicleDataStore
    {
        private readonly InMemoryVehicleStore _vehicleStore = new();
        private readonly InMemoryMileageRecordStore _mileageStore = new();

        // New EV session stores
        private readonly InMemoryBaseEvChargeSessionStore _baseEvStore;
        private readonly InMemoryCommercialEvChargeSessionStore _commercialEvStore;
        private readonly InMemoryHomeEvChargeSessionStore _homeEvStore;
        private readonly InMemoryHomeEvChargeAttributionStore _attribStore;
        private readonly InMemoryHomeEvChargeBillingStore _billingStore;

        // Existing ICE + maintenance stores
        private readonly InMemoryIceMileageStore _iceMileageStore;
        private readonly InMemoryMaintenanceInvoiceStore _maintenanceStore = new();

        public InMemoryVehicleDataStore()
        {
            _iceMileageStore = new InMemoryIceMileageStore(_mileageStore);

            // NEW EV stores
            _baseEvStore = new InMemoryBaseEvChargeSessionStore(_mileageStore);
            _commercialEvStore = new InMemoryCommercialEvChargeSessionStore();
            _homeEvStore = new InMemoryHomeEvChargeSessionStore();
            _attribStore = new InMemoryHomeEvChargeAttributionStore();
            _billingStore = new InMemoryHomeEvChargeBillingStore();

            // Seed vehicles
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

        public void UpsertIceMileageRecord(IceMileageRecord record) =>
            _iceMileageStore.Upsert(record);

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
        // BASE EV CHARGE SESSIONS
        // ---------------------------------------------------------

        public void UpsertBaseEvChargeSession(BaseEvChargeSession session) =>
            _baseEvStore.Upsert(session);

        public BaseEvChargeSession? GetBaseEvChargeSession(Guid sessionId) =>
            _baseEvStore.Get(sessionId);

        public IEnumerable<BaseEvChargeSession> GetBaseEvChargeSessions(Guid vehicleId, DateTime start, DateTime end) =>
            _baseEvStore.GetRange(vehicleId, start, end);

        public BaseEvChargeSession? GetLatestBaseEvChargeSession(Guid vehicleId) =>
            _baseEvStore.GetLatest(vehicleId);

        public void DeleteBaseEvChargeSession(Guid sessionId) =>
            _baseEvStore.Delete(sessionId);

        // ---------------------------------------------------------
        // COMMERCIAL EV CHARGE SESSIONS
        // ---------------------------------------------------------

        public void UpsertCommercialEvChargeSession(CommercialEvChargeSession session) =>
            _commercialEvStore.Upsert(session);

        public CommercialEvChargeSession? GetCommercialEvChargeSession(Guid sessionId) =>
            _commercialEvStore.Get(sessionId);

        // ---------------------------------------------------------
        // HOME EV CHARGE SESSIONS
        // ---------------------------------------------------------

        public void UpsertHomeEvChargeSession(HomeEvChargeSession session) =>
            _homeEvStore.Upsert(session);

        public HomeEvChargeSession? GetHomeEvChargeSession(Guid sessionId) =>
            _homeEvStore.Get(sessionId);

        // ---------------------------------------------------------
        // HOME EV CHARGE ATTRIBUTION
        // ---------------------------------------------------------

        public void UpsertHomeEvChargeAttribution(Guid sessionId, HomeEvChargeAttribution attribution) =>
            _attribStore.Upsert(sessionId, attribution);

        public HomeEvChargeAttribution? GetHomeEvChargeAttribution(Guid sessionId) =>
            _attribStore.Get(sessionId);

        // ---------------------------------------------------------
        // HOME EV CHARGE BILLING
        // ---------------------------------------------------------

        public void UpsertHomeEvChargeBilling(Guid sessionId, HomeEvChargeBilling billing) =>
            _billingStore.Upsert(sessionId, billing);

        public HomeEvChargeBilling? GetHomeEvChargeBilling(Guid sessionId) =>
            _billingStore.Get(sessionId);

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
