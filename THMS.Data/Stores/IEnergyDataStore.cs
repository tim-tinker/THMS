using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores
{
    public interface IEnergyDataStore
    {
        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        /// <summary>Upsert key: <see cref="HomeCircuitReading.Timestamp"/>.</summary>
        void UpsertHomeCircuitReading(HomeCircuitReading reading);

        IEnumerable<HomeCircuitReading> GetHomeCircuitReadings(
            DateTime start,
            DateTime end);

        /// <summary>Most recent reading by <see cref="HomeCircuitReading.Timestamp"/>, or null if none.</summary>
        HomeCircuitReading? GetLatestHomeCircuitReading();

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
        // ENERGY ATTRIBUTION RESULTS
        // ---------------------------------------------------------

        /// <summary>Upsert key: <see cref="HomeCircuitAttribution.Timestamp"/>.</summary>
        void UpsertHomeCircuitAttribution(HomeCircuitAttribution result);

        IReadOnlyCollection<HomeCircuitAttribution> GetHomeCircuitAttribution(DateTime start, DateTime end);

        /// <summary>Most recent result by <see cref="HomeCircuitAttribution.Timestamp"/>, or null if none.</summary>
        HomeCircuitAttribution? GetLatestHomeCircuitAttribution();

    }
}
