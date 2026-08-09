using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores
{
    public interface IEnergyDataStore
    {
        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        /// <summary>Upsert key: <see cref="EvCircuitReading.Timestamp"/>.</summary>
        void UpsertEvCircuitReading(EvCircuitReading reading);

        IEnumerable<EvCircuitReading> GetEvCircuitReadings(
            DateTime start,
            DateTime end);

        /// <summary>Most recent reading by <see cref="EvCircuitReading.Timestamp"/>, or null if none.</summary>
        EvCircuitReading? GetLatestEvCircuitReading();

        // ---------------------------------------------------------
        // HOME SOLAR VENDOR INTERVALS
        // ---------------------------------------------------------

        /// <summary>Upsert key: <see cref="SolarVendorInterval.Timestamp"/>.</summary>
        void UpsertSolarVendorInterval(SolarVendorInterval interval);

        IEnumerable<SolarVendorInterval> GetSolarVendorIntervals(
            DateTime start,
            DateTime end);

        /// <summary>Most recent interval by <see cref="SolarVendorInterval.Timestamp"/>, or null if none.</summary>
        SolarVendorInterval? GetLatestSolarVendorInterval();


        // ---------------------------------------------------------
        // COMMERCIAL CHARGING SESSIONS
        // ---------------------------------------------------------

        /// <summary>Upsert key: <see cref="EvCommercialChargeSession.EndTime"/>.</summary>
        void UpsertEvCommercialChargeSession(EvCommercialChargeSession session);

        IEnumerable<EvCommercialChargeSession> GetEvCommercialChargeSessions(
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

        /// <summary>Upsert key: <see cref="EnergyAttributionResult.Timestamp"/>.</summary>
        void UpsertEvAttribution(EnergyAttributionResult result);

        IReadOnlyCollection<EnergyAttributionResult> GetEvAttribution(DateTime start, DateTime end);

        /// <summary>Most recent result by <see cref="EnergyAttributionResult.Timestamp"/>, or null if none.</summary>
        EnergyAttributionResult? GetLatestEvAttribution();

    }
}
