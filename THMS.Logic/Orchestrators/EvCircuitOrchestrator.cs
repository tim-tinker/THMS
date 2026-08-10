using THMS.Data.Stores;
using THMS.Ingestion.Importers.Energy;
using THMS.Logic.Energy;

namespace THMS.Logic.Orchestrators
{
    public class EvCircuitOrchestrator
    {
        private readonly IEnergyDataStore _energyStore;

        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int ReadingCount { get; private set; }
        public string ErrorMessage { get; private set; }

        public EvCircuitOrchestrator(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void Update(string filePath)
        {
            var importer = new HomeEvCircuitImporter(_energyStore);
            importer.Import(filePath);
            StartDate = importer.StartDate;
            EndDate = importer.EndDate;
            ReadingCount = importer.ReadingCount;
            ErrorMessage = importer.ErrorMessage;

            if (string.IsNullOrEmpty(importer.ErrorMessage) && 0 < importer.ReadingCount)
            {
                foreach (var reading in importer.Readings)
                {
                    _energyStore.UpsertEvCircuitReading(reading);
                }

                CalculateEvAttribution(StartDate, EndDate);
            }
        }

        private void CalculateEvAttribution(DateTime start, DateTime end)
        {
            var engine = new EvAttributionEngine(_energyStore);
            engine.Compute(start, end);

            foreach (var result in engine.Results)
            {
                _energyStore.UpsertEvAttribution(result);
            }
        }
    }
}
