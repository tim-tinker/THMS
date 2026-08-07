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
        void AddEvChargeSession(EvChargeSession session);

        // Read (single)
        EvChargeSession? GetEvChargeSession(Guid sessionId);

        // Read (filtered by period)
        IEnumerable<EvChargeSession> GetEvChargeSessions(Guid vehicleId, DateTime start, DateTime end);

        // Update
        void UpdateEvChargeSession(EvChargeSession session);

        // Delete (optional)
        void DeleteEvChargeSession(Guid sessionId);

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------
        void AddMaintenanceInvoice(MaintenanceInvoiceRecord invoice);
        IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end);

        decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end);
    }
}
