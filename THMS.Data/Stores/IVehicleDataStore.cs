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
        IceMileageRecord? GetEarliestIceMileageRecord(Guid vehicleId);
        IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end);

        // Aggregated mileage (used by TransportationAnalyticsEngine)
        decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end);

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS (vehicle‑assigned)
        // ---------------------------------------------------------
        // Create
        void AddEvChargingSession(EvChargingSession session);

        // Read (single)
        EvChargingSession? GetEvChargingSession(Guid sessionId);

        // Read (filtered by period)
        IEnumerable<EvChargingSession> GetEvChargingSessions(Guid vehicleId, DateTime start, DateTime end);

        // Update
        void UpdateEvChargingSession(EvChargingSession session);

        // Delete (optional)
        void DeleteEvChargingSession(Guid sessionId);

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------
        void AddMaintenanceInvoice(MaintenanceInvoiceRecord invoice);
        IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end);

        decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end);
    }
}
