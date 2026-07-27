using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Ingestion.Importers.Energy
{
    public abstract class CommercialChargerImporter
    {
        protected IEnergyDataStore? EnergyStore { get; set; }
        protected IFinanceDataStore? FinanceStore { get; set; }

        public void Import(string filePath)
        {
            if (EnergyStore is null)
            {
                throw new InvalidOperationException("IEnergyDataStore is not set. Cannot import data.");
            }

            if (FinanceStore is null)
            {
                throw new InvalidOperationException("IFinanceStore is not set. Cannot import data.");
            }

            foreach (var entry in ReadChargingSessions(filePath))
            {
                var chargingSession = entry.Item1;
                var costRecord = entry.Item2;
                EnergyStore.AddEvCommercialChargingSession(chargingSession);
                FinanceStore.AddCommercialChargingCostRecord(costRecord);
            }
        }

        protected abstract IEnumerable<Tuple<EvCommercialChargingSession, CommercialChargingCostRecord>> ReadChargingSessions(string filePath);
   }
}
