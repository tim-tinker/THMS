using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Ingestion.Importers.Energy
{
    public abstract class CommericalChargerImporter
    {
        protected IEnergyDataStore? Store { get; set; }

        public void Import(string csvPath)
        {
            if (Store is null)
            {
                throw new InvalidOperationException("IEnergyDataStore is not set. Cannot import data.");
            }

            foreach (var entry in ReadChargingSessions(csvPath))
            {
                Store.AddEvChargingSession(entry);
            }
        }

        protected abstract IEnumerable<EvChargingSession> ReadChargingSessions(string csvPath);
   }
}
