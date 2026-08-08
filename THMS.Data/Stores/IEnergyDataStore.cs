using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores
{
    public interface IEnergyDataStore
    {
        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        void AddEvCircuitReading(EvCircuitReading reading);

        IEnumerable<EvCircuitReading> GetEvCircuitReadings(
            DateTime start,
            DateTime end);

        // ---------------------------------------------------------
        // HOME SOLAR VENDOR INTERVALS
        // ---------------------------------------------------------

        void AddSolarVendorInterval(SolarVendorInterval interval);

        IEnumerable<SolarVendorInterval> GetSolarVendorIntervals(
            DateTime start,
            DateTime end);


        // ---------------------------------------------------------
        // COMMERCIAL CHARGING SESSIONS
        // ---------------------------------------------------------

        void AddEvCommercialChargeSession(EvCommercialChargeSession session);

        IEnumerable<EvCommercialChargeSession> GetEvCommercialChargeSessions(
            DateTime start,
            DateTime end);


        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS
        // ---------------------------------------------------------

        void AddCommercialChargeCostRecord(CommercialChargeCostRecord record);

        IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecords(
            DateTime start,
            DateTime end);

        IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecordsByVendor(
            string vendor,
            DateTime start,
            DateTime end);

        // ----------------------------------------------------------
        // HOME EV CIRCUIT SEGMENTS
        // ----------------------------------------------------------

        // Store segments for a session (overwrite existing)
        void SaveEvCircuitSegments(Guid sessionId, IEnumerable<EvCircuitSegment> segments);

        // Retrieve segments for a session
        IEnumerable<EvCircuitSegment> GetEvCircuitSegments(Guid sessionId);

        // Delete all segments for a session
        void DeleteEvCircuitSegments(Guid sessionId);

        // Optional convenience: roll-up summary
        EvCircuitSegmentSummary GetEvCircuitSummary(Guid sessionId);

        // ---------------------------------------------------------
        // ENERGY ATTRIBUTION RESULTS
        // ---------------------------------------------------------

        void AddEvAttribution(EnergyAttributionResult result);

        IReadOnlyCollection<EnergyAttributionResult> GetEvAttribution(DateTime start, DateTime end);

    }
}
