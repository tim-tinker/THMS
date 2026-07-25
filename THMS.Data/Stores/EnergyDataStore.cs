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
    public class EnergyDataStore
    {
        // -----------------------------
        // EV CHARGING INTERVALS
        // -----------------------------

        private readonly List<EvCircuitReading> _evChargingIntervals =
            new List<EvCircuitReading>();

        public void AddEvCircuitReading(EvCircuitReading interval)
        {
            _evChargingIntervals.Add(interval);
        }

        public IReadOnlyCollection<EvCircuitReading> GetEvChargingIntervals() =>
            _evChargingIntervals.OrderBy(i => i.Timestamp).ToList().AsReadOnly();

        // -----------------------------
        // SOLAR VENDOR INTERVALS
        // -----------------------------

        private readonly List<SolarVendorInterval> _solarVendorIntervals =
            new List<SolarVendorInterval>();

        public void AddSolarVendorInterval(SolarVendorInterval interval)
        {
            _solarVendorIntervals.Add(interval);
        }

        public IReadOnlyCollection<SolarVendorInterval> GetSolarVendorIntervals() =>
            _solarVendorIntervals.OrderBy(i => i.Timestamp).ToList().AsReadOnly();

        // -----------------------------
        // RAW ACCESSORS FOR LOGIC ENGINES
        // -----------------------------

        public IReadOnlyCollection<EvCircuitReading> GetAllEvChargingIntervalsRaw() =>
            _evChargingIntervals.AsReadOnly();

        public IReadOnlyCollection<SolarVendorInterval> GetAllSolarVendorIntervalsRaw() =>
            _solarVendorIntervals.AsReadOnly();
    }
}
