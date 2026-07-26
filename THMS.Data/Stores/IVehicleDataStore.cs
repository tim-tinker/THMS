using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public interface IVehicleDataStore
    {
        // -------------------------------
        // VEHICLES
        // -------------------------------

        VehicleBase? GetVehicle(Guid vehicleId);
        IEnumerable<VehicleBase> GetAllVehicles();
        void AddVehicle(VehicleBase vehicle);

        // -------------------------------
        // ICE MILEAGE RECORDS
        // -------------------------------

        IEnumerable<IceMileageRecord> GetIceMileageRecords(
            Guid vehicleId,
            DateTime start,
            DateTime end);

        void AddIceMileageRecord(IceMileageRecord record);

        // -------------------------------
        // EV CHARGING SESSIONS (Transportation)
        // -------------------------------

        /// <summary>
        /// Returns EV charging sessions that have vehicle data assigned.
        /// </summary>
        IEnumerable<EvChargingSession> GetEvChargingSessions(
            Guid vehicleId,
            DateTime start,
            DateTime end);

        /// <summary>
        /// Returns all EV charging sessions, including unassigned ones.
        /// </summary>
        IEnumerable<EvChargingSession> GetAllEvChargingSessions();

        void AddEvChargingSession(EvChargingSession session);

        // -------------------------------
        // EV CHARGING SESSION VEHICLE DATA
        // -------------------------------

        EvChargingSessionVehicleData? GetEvChargingSessionVehicleData(Guid id);

        void AddEvChargingSessionVehicleData(EvChargingSessionVehicleData data);

        // -------------------------------
        // SESSION ENRICHMENT WORKFLOW
        // -------------------------------

        /// <summary>
        /// Assigns vehicle data to a charging session.
        /// Sets session.VehicleDataId = vehicleDataId.
        /// </summary>
        void AttachVehicleDataToChargingSession(
            Guid sessionId,
            Guid vehicleDataId);
    }
}
