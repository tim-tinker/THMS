using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Ingestion.Importers.Energy
{
    public abstract class BaseCommercialChargerImporter
    {
        protected IEnergyDataStore? EnergyStore { get; set; }
        protected IVehicleDataStore? VehicleStore { get; set; }
        protected IFinanceDataStore? FinanceStore { get; set; }

        public void Import(string filePath)
        {
            if (VehicleStore is null)
            {
                throw new InvalidOperationException("IVehicleDataStore is not set. Cannot import data.");
            }

            if (FinanceStore is null)
            {
                throw new InvalidOperationException("IFinanceStore is not set. Cannot import data.");
            }

            foreach (var chargeSession in ReadChargeSessions(filePath))
            {
                VehicleStore.UpsertCommercialEvChargeSession(chargeSession);
            }
        }

        protected abstract IEnumerable<CommercialEvChargeSession> ReadChargeSessions(string filePath);
   }
}
