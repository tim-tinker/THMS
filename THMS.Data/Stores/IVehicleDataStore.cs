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
        // BASE EV CHARGE SESSIONS (common fields)
        // ---------------------------------------------------------
        void UpsertBaseEvChargeSession(BaseEvChargeSession session);
        BaseEvChargeSession? GetBaseEvChargeSession(Guid sessionId);
        IEnumerable<BaseEvChargeSession> GetBaseEvChargeSessions(Guid vehicleId, DateTime start, DateTime end);
        /// <summary>Most recent session by <see cref="BaseEvChargeSession.EndTime"/> across all vehicles, or null if none.</summary>
        BaseEvChargeSession? GetLatestBaseEvChargeSession();
        BaseEvChargeSession? GetLatestBaseEvChargeSession(Guid vehicleId);
        void DeleteBaseEvChargeSession(Guid sessionId);

        // ---------------------------------------------------------
        // COMMERCIAL EV CHARGE SESSIONS (complete at creation)
        // ---------------------------------------------------------
        void UpsertCommercialEvChargeSession(CommercialEvChargeSession session);
        CommercialEvChargeSession? GetCommercialEvChargeSession(Guid sessionId);

        // ---------------------------------------------------------
        // HOME EV CHARGE SESSIONS (incrementally completed)
        // ---------------------------------------------------------
        void UpsertHomeEvChargeSession(HomeEvChargeSession session);
        HomeEvChargeSession? GetHomeEvChargeSession(Guid sessionId);

        // ---------------------------------------------------------
        // HOME EV CHARGE ATTRIBUTION (solar + circuit data)
        // ---------------------------------------------------------
        void UpsertHomeEvChargeAttribution(Guid sessionId, HomeEvChargeAttribution attribution);
        HomeEvChargeAttribution? GetHomeEvChargeAttribution(Guid sessionId);

        // ---------------------------------------------------------
        // HOME EV CHARGE BILLING (utility bill data)
        // ---------------------------------------------------------
        void UpsertHomeEvChargeBilling(Guid sessionId, HomeEvChargeBilling billing);
        HomeEvChargeBilling? GetHomeEvChargeBilling(Guid sessionId);

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------
        /// <summary>Upsert key: <see cref="MaintenanceInvoiceRecord.Id"/>.</summary>
        void UpsertMaintenanceInvoice(MaintenanceInvoiceRecord invoice);
        IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end);
        decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end);
    }
}
