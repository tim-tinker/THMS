using THMS.Data.Stores;
using THMS.Ingestion.Importers.Transportation;

namespace THMS.Ingestion
{
    /// <summary>
    /// Coordinates ingestion of transportation-related data.
    /// Currently empty until importers are implemented.
    /// </summary>
    public class TransportIngestionPipeline
    {
        private readonly TransportationDataStore _store;

        public TransportIngestionPipeline(TransportationDataStore store)
        {
            _store = store;
        }

        // Future:
        // public void IngestMileage(string csvPath) { ... }
        // public void IngestChargingCosts(string csvPath) { ... }
        // public void IngestGasReceipts(string csvPath) { ... }
        // public void IngestMaintenanceInvoices(string csvPath) { ... }
    }
}
