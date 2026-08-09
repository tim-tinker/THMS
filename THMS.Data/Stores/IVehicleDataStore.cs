using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public interface IVehicleDataStore
    {
        // ---------------------------------------------------------
        // VEHICLES
        // ---------------------------------------------------------
        /// <summary>Upsert key: <see cref="VehicleBase.Id"/>.</summary>
        void UpsertVehicle(VehicleBase vehicle);
        VehicleBase? GetVehicle(Guid id);
        IEnumerable<VehicleBase> GetAllVehicles();

        // ---------------------------------------------------------
        // ICE MILEAGE
        // ---------------------------------------------------------
        /// <summary>Upsert key: <see cref="IceMileageRecord.Id"/>.</summary>
        void UpsertIceMileageRecord(IceMileageRecord record);
        IceMileageRecord? GetEarliestIceMileageRecord(Guid vehicleId);
        IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end);

        // ---------------------------------------------------------
        // SHARED MILEAGE (ICE + EV)
        // ---------------------------------------------------------
        decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end);

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS (vehicle‑assigned)
        // ---------------------------------------------------------
        /// <summary>Upsert key: <see cref="EvChargeSession.Id"/>.</summary>
        void UpsertEvChargeSession(EvChargeSession session);
        EvChargeSession? GetEvChargeSession(Guid sessionId);
        IEnumerable<EvChargeSession> GetEvChargeSessions(Guid vehicleId, DateTime start, DateTime end);
        /// <summary>Most recent session by <see cref="EvChargeSession.EndTime"/>, or null if none.</summary>
        EvChargeSession? GetLatestEvChargeSession();
        void DeleteEvChargeSession(Guid sessionId);

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------
        /// <summary>Upsert key: <see cref="MaintenanceInvoiceRecord.Id"/>.</summary>
        void UpsertMaintenanceInvoice(MaintenanceInvoiceRecord invoice);
        IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end);
        decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end);
    }
}
