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

        void AddEvCommercialChargingSession(EvCommercialChargingSession session);

        IEnumerable<EvCommercialChargingSession> GetEvCommercialChargingSessions(
            DateTime start,
            DateTime end);


        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS
        // ---------------------------------------------------------

        void AddCommercialChargingCostRecord(CommercialChargingCostRecord record);

        IEnumerable<CommercialChargingCostRecord> GetCommercialChargingCostRecords(
            DateTime start,
            DateTime end);

        IEnumerable<CommercialChargingCostRecord> GetCommercialChargingCostRecordsByVendor(
            string vendor,
            DateTime start,
            DateTime end);
    }
}
