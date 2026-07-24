using THMS.Domain.Energy;
using THMS.Domain.Finance.Billing;

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
        // HOME ENERGY FLOW INTERVALS
        // -----------------------------

        private readonly List<HomeEnergyFlowInterval> _homeEnergyFlows =
            new List<HomeEnergyFlowInterval>();

        public void AddHomeEnergyFlow(HomeEnergyFlowInterval interval)
        {
            _homeEnergyFlows.Add(interval);
        }

        public IReadOnlyCollection<HomeEnergyFlowInterval> GetHomeEnergyFlows() =>
            _homeEnergyFlows.OrderBy(i => i.Timestamp).ToList().AsReadOnly();

        // -----------------------------
        // EV CHARGING INTERVALS
        // -----------------------------

        private readonly List<EvChargingInterval> _evChargingIntervals =
            new List<EvChargingInterval>();

        public void AddEvChargingInterval(EvChargingInterval interval)
        {
            _evChargingIntervals.Add(interval);
        }

        public IReadOnlyCollection<EvChargingInterval> GetEvChargingIntervals() =>
            _evChargingIntervals.OrderBy(i => i.Timestamp).ToList().AsReadOnly();

        // -----------------------------
        // BILLING COST INTERVALS
        // -----------------------------

        private readonly List<ElectricUtilityBillCostInterval> _electricUtilityBillIntervals  =
            new List<ElectricUtilityBillCostInterval>();

        public void AddElectricUtilityBillCostInterval(ElectricUtilityBillCostInterval interval)
        {
            _electricUtilityBillIntervals .Add(interval);
        }

        public IReadOnlyCollection<ElectricUtilityBillCostInterval> GetElectricUtilityBillCostIntervals() =>
            _electricUtilityBillIntervals .OrderBy(i => i.Start).ToList().AsReadOnly();

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

        public IReadOnlyCollection<HomeEnergyFlowInterval> GetAllHomeEnergyFlowsRaw() =>
            _homeEnergyFlows.AsReadOnly();

        public IReadOnlyCollection<EvChargingInterval> GetAllEvChargingIntervalsRaw() =>
            _evChargingIntervals.AsReadOnly();

        public IReadOnlyCollection<ElectricUtilityBillCostInterval> GetAllBillingCostIntervalsRaw() =>
            _electricUtilityBillIntervals .AsReadOnly();

        public IReadOnlyCollection<SolarVendorInterval> GetAllSolarVendorIntervalsRaw() =>
            _solarVendorIntervals.AsReadOnly();
    }
}
