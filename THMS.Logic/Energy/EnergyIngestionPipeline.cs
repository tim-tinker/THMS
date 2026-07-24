using THMS.Data.Stores;
using THMS.Importers.Energy;
using THMS.Logic.Importers.Energy;
using THMS.Logic.ViewModels.Importers.Finance;

namespace THMS.Logic.Energy
{
    /// <summary>
    /// Coordinates ingestion of all raw energy data sources:
    /// - SPAN home energy flow intervals
    /// - EV charging intervals
    /// - Billing cost intervals
    /// - Solar vendor intervals
    ///
    /// Populates the unified EnergyDataStore with raw domain objects.
    /// </summary>
    public class EnergyIngestionPipeline
    {
        private readonly EnergyDataStore _store;

        public EnergyIngestionPipeline(EnergyDataStore store)
        {
            _store = store;
        }

        public void IngestSpanData(string spanCsvPath)
        {
            var importer = new SpanEnergyImporter(_store);
            importer.Import(spanCsvPath);
        }

        public void IngestEvChargingData(string chargingCsvPath)
        {
            var importer = new EvChargerImporter(_store);
            importer.Import(chargingCsvPath);
        }

        public void IngestBillingData(string billingCsvPath)
        {
            var importer = new ElectricUtilityBillImporter(_store);
            importer.Import(billingCsvPath);
        }

        public void IngestSolarVendorData(string solarCsvPath)
        {
            var importer = new SolarReportImporter(_store);
            importer.Import(solarCsvPath);
        }
    }
}
