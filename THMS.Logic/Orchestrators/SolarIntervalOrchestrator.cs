using THMS.Data.Stores;
using THMS.Ingestion.Importers.Energy;
using THMS.Logic.Energy;

namespace THMS.Logic.Orchestrators
{
    public class SolarIntervalOrchestrator
    {
        private readonly IEnergyDataStore _energyStore;

        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int IntervalCount { get; private set; }
        public string ErrorMessage { get; private set; }

        public SolarIntervalOrchestrator(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void Update(string filePath)
        {

            var importer = new EnphaseSolarImporter(_energyStore);
            importer.Import(filePath);
            StartDate = importer.StartDate;
            EndDate = importer.EndDate;
            IntervalCount = importer.IntervalCount;
            ErrorMessage = importer.ErrorMessage;

            if (string.IsNullOrEmpty(importer.ErrorMessage) && 0 < importer.IntervalCount)
            {
                foreach (var interval in importer.Intervals)
                {
                    _energyStore.UpsertSolarVendorInterval(interval);
                }

                CalculateEvAttribution(StartDate, EndDate);
            }
        }

        private void CalculateEvAttribution(DateTime start, DateTime end)
        {
            if (_energyStore.GetEvCircuitReadings(start, end).Any())
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
}
