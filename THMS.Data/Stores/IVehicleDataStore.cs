using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public interface IVehicleDataStore
    {
        // ---------------------------------------------------------
        // VEHICLES
        // ---------------------------------------------------------
        void AddVehicle(VehicleBase vehicle);
        VehicleBase? GetVehicle(Guid id);
        IEnumerable<VehicleBase> GetAllVehicles();

        // ---------------------------------------------------------
        // ICE MILEAGE
        // ---------------------------------------------------------
        void AddIceMileageRecord(IceMileageRecord record);
        IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end);

        // Aggregated mileage (used by TransportationAnalyticsEngine)
        decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end);

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS (vehicle‑assigned)
        // ---------------------------------------------------------
        void AddEvChargingSession(EvChargingSession session);
        IEnumerable<EvChargingSession> GetEvChargingSessions(Guid vehicleId, DateTime start, DateTime end);

        // Unassigned sessions (for reconciliation UI)
        IEnumerable<EvChargingSession> GetUnassignedEvChargingSessions();

        // ---------------------------------------------------------
        // EV CHARGING SESSION VEHICLE DATA (partial + update)
        // ---------------------------------------------------------
        void AddEvChargingSessionVehicleData(EvChargingSessionVehicleData data);
        void UpdateEvChargingSessionVehicleData(EvChargingSessionVehicleData data);
        IEnumerable<EvChargingSessionVehicleData> GetEvChargingSessionVehicleData(Guid vehicleId, DateTime start, DateTime end);
        EvChargingSessionVehicleData? GetEvChargingSessionVehicleDataById(Guid id);

        // Link session ↔ vehicle data
        void AttachVehicleDataToChargingSession(Guid sessionId, Guid vehicleDataId);

        // ---------------------------------------------------------
        // EV CHARGING COST RECORDS (vehicle attribution)
        // ---------------------------------------------------------
        void AddChargingCostRecord(ChargingCostRecord record);
        IEnumerable<ChargingCostRecord> GetChargingCosts(Guid vehicleId, DateTime start, DateTime end);

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------
        void AddMaintenanceInvoice(MaintenanceInvoiceRecord invoice);
        IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end);

        decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end);
    }
}
