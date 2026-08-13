using THMS.Data.Stores;
using THMS.Ingestion.Importers.Energy;

namespace THMS.Ingestion
{
    public class EnergyIngestionPipeline
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly IFinanceDataStore _financeStore;
        private readonly IVehicleDataStore _vehicleStore;

        public EnergyIngestionPipeline(
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore,
            IVehicleDataStore vehicleStore)
        {
            _energyStore = energyStore;
            _financeStore = financeStore;
            _vehicleStore = vehicleStore;
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING (ChargePoint)
        // ---------------------------------------------------------

        public void IngestChargePointData(string filePath)
        {
            var importer = new ChargePointImporter(_vehicleStore, _financeStore);
            importer.Import(filePath);
        }

        // ---------------------------------------------------------
        // HOME EV CIRCUIT (SPAN)
        // ---------------------------------------------------------

        public void IngestHomeCircuitData(string filePath)
        {
            var importer = new HomeCircuitImporter(_energyStore);
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
