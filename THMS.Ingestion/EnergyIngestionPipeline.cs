using THMS.Data.Stores;
using THMS.Ingestion.Importers.Energy;

namespace THMS.Ingestion
{
    public class EnergyIngestionPipeline
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly IFinanceDataStore _financeStore;

        public EnergyIngestionPipeline(
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore)
        {
            _energyStore = energyStore;
            _financeStore = financeStore;
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING (ChargePoint)
        // ---------------------------------------------------------

        public void IngestChargePointData(string filePath)
        {
            var importer = new ChargePointImporter(_energyStore, _financeStore);
            importer.Import(filePath);
        }

        // ---------------------------------------------------------
        // HOME EV CIRCUIT (SPAN)
        // ---------------------------------------------------------

        public void IngestHomeEvCircuitData(string filePath)
        {
            var importer = new HomeEvCircuitImporter(_energyStore);
            importer.Import(filePath);
        }

        // ---------------------------------------------------------
        // SOLAR VENDOR (Enphase)
        // ---------------------------------------------------------

        public void IngestEnphaseSolarData(string filePath)
        {
            var importer = new EnphaseSolarImporter(_energyStore);
            importer.Import(filePath);
        }
    }
}
