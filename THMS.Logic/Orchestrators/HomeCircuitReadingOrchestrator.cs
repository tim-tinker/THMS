using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Ingestion.Importers.Energy;
using THMS.Logic.Energy;

namespace THMS.Logic.Orchestrators
{
    public class HomeCircuitReadingOrchestrator : BaseOrchestrator
    {
        private readonly IEnergyDataStore _energyStore;

        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int ReadingCount { get; private set; }
        public string ErrorMessage { get; private set; }

        public HomeCircuitReadingOrchestrator()
            : this(new DataStoreFactory().GetEnergyStore())
        {
        }

        public HomeCircuitReadingOrchestrator(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void Update(string filePath)
        {
            var importer = new HomeCircuitImporter(_energyStore);
            importer.Import(filePath);
            StartDate = importer.StartDate;
            EndDate = importer.EndDate;
            ReadingCount = importer.ReadingCount;
            ErrorMessage = importer.ErrorMessage;

            if (string.IsNullOrEmpty(importer.ErrorMessage) && 0 < importer.ReadingCount)
            {
                foreach (var reading in importer.Readings)
                {
                    _energyStore.UpsertHomeCircuitReading(reading);
                }

                CalculateEvAttribution(StartDate, EndDate);
            }
        }

        private void CalculateEvAttribution(DateTime start, DateTime end)
        {
            var engine = new HomeCircuitAttributionEngine(_energyStore);
            engine.Compute(start, end);

            foreach (var result in engine.Results)
            {
                _energyStore.UpsertHomeCircuitAttribution(result);
            }
        }

        public IEnumerable<HomeCircuitReading> GetHomeCircuitReadings(string period)
        {
            var readings = Array.Empty<HomeCircuitReading>();
            var latest = _energyStore.GetLatestHomeCircuitReading();
            if (latest is not null)
            {
                var end = latest.Timestamp;
                var start = GetStartDate(end, period);
                readings = _energyStore.GetHomeCircuitReadings(start, end).ToArray();
            }

            return readings;
        }
    }
}
