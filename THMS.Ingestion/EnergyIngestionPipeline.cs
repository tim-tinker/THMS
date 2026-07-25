using THMS.Data.Stores;
using THMS.Ingestion.Importers.Energy;
using THMS.Ingestion.Importers.Finance;

namespace THMS.Ingestion
{
    /// <summary>
    /// Coordinates ingestion of all raw energy data sources.
    /// Populates the unified EnergyDataStore with domain objects.
    /// </summary>
    public class EnergyIngestionPipeline
    {
        private readonly IEnergyDataStore _store;

        public EnergyIngestionPipeline(IEnergyDataStore store)
        {
            _store = store;
        }

        public void IngestSpanData(string spanCsvPath)
        {
            var importer = new HomeEvCircuitImporter(_store);
            importer.Import(spanCsvPath);
        }

        public void IngestEvChargingData(string chargingCsvPath)
        {
            var importer = new ChargePointImporter(_store);
            importer.Import(chargingCsvPath);
        }

        public void IngestSolarVendorData(string solarCsvPath)
        {
            var importer = new SolarReportImporter(_store);
            importer.Import(solarCsvPath);
        }
    }
}
