using THMS.Domain.Energy;

namespace THMS.Data.Stores
{
    /// <summary>
    /// Stores all raw energy-related domain objects produced by the
    /// EnergyIngestionPipeline. This is the single source of truth for
    /// energy data in THMS.
    ///
    /// Normalization is performed on-demand by logic engines.
    /// </summary>
    public interface IEnergyDataStore
    {
        void AddEvCircuitReading(EvCircuitReading interval);
        IReadOnlyCollection<EvCircuitReading> GetEvCircuitReadings();

        void AddEvChargingSession(EvCommercialChargingSession session);
        IReadOnlyCollection<EvCommercialChargingSession> GetEvChargingSessions();

        void AddSolarVendorInterval(SolarVendorInterval interval);
        IReadOnlyCollection<SolarVendorInterval> GetSolarVendorIntervals();

        IReadOnlyCollection<EvCircuitReading> GetAllEvCircuitReadingsRaw();

        IReadOnlyCollection<SolarVendorInterval> GetAllSolarVendorIntervalsRaw();
    }
}
