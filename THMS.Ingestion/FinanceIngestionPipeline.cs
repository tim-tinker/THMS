using THMS.Data.Stores;
using THMS.Ingestion.Importers.Finance;

namespace THMS.Ingestion
{
    public class FinanceIngestionPipeline
    {
        private readonly IFinanceDataStore _store;

        public FinanceIngestionPipeline(IFinanceDataStore store)
        {
            _store = store;
        }

        public void IngestElectricUtilityBill(string billingCsvPath)
        {
            var importer = new ElectricUtilityBillImporter(_store);
            importer.Import(billingCsvPath);
        }
    }
}
