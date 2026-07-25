namespace THMS.Data.Stores
{
    using THMS.Domain.Transportation;

    /// <summary>
    /// Provides persistence operations for vehicle mileage records,
    /// supporting both ICE and EV vehicles.
    /// </summary>
    public interface IVehicleDataStore
    {
        // ---------------------------------------------------------
        // ICE MILEAGE RECORDS
        // ---------------------------------------------------------

        /// <summary>
        /// Adds a new ICE mileage record (fuel fill-up).
        /// </summary>
        void AddIceMileageRecord(IceMileageRecord record);

        /// <summary>
        /// Retrieves all ICE mileage records for a given vehicle.
        /// </summary>
        IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId);

        /// <summary>
        /// Retrieves ICE mileage records for a vehicle within a date range.
        /// </summary>
        IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end);

        // ---------------------------------------------------------
        // EV MILEAGE RECORDS
        // ---------------------------------------------------------

        /// <summary>
        /// Adds a new EV mileage record (charging session).
        /// </summary>
        void AddEvMileageRecord(EvMileageRecord record);

        /// <summary>
        /// Retrieves all EV mileage records for a given vehicle.
        /// </summary>
        IEnumerable<EvMileageRecord> GetEvMileageRecords(Guid vehicleId);

        /// <summary>
        /// Retrieves EV mileage records for a vehicle within a date range.
        /// </summary>
        IEnumerable<EvMileageRecord> GetEvMileageRecords(Guid vehicleId, DateTime start, DateTime end);

        // ---------------------------------------------------------
        // GENERAL
        // ---------------------------------------------------------

        /// <summary>
        /// Deletes a mileage record (EV or ICE) by its ID.
        /// </summary>
        void DeleteMileageRecord(Guid recordId);
    }
}
