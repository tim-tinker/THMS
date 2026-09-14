using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Ingestion.Importers.Energy;
using THMS.Logic.Energy;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Energy;

namespace THMS.Logic.Orchestrators
{
    public class HomeCircuitReadingOrchestrator : BaseOrchestrator
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly HomeCircuitImporter _importer = new();

        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int ReadingCount { get; private set; }
        public string ErrorMessage { get; private set; } = "";

        public HomeCircuitReadingOrchestrator()
            : this(new DataStoreFactory().GetEnergyStore())
        {
        }

        public HomeCircuitReadingOrchestrator(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public List<HomeCircuitReadingImportPreview> LoadReadingsFromFiles(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            var rows = new List<HomeCircuitReadingImportPreview>();
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("A file path is required.");
                if (!File.Exists(path))
                    throw new FileNotFoundException("The selected file was not found.", path);

                foreach (var reading in _importer.Parse(path))
                {
                    rows.Add(new HomeCircuitReadingImportPreview
                    {
                        Timestamp = reading.Timestamp,
                        KiloWattHours = reading.KiloWattHours
                    });
                }
            }

            return rows.OrderBy(row => row.Timestamp).ToList();
        }

        public ImportResult ImportReadings(IEnumerable<HomeCircuitReadingImportPreview> previewRows) =>
            ImportReadings(previewRows, progress: null);

        public ImportResult ImportReadings(
            IEnumerable<HomeCircuitReadingImportPreview> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows as IReadOnlyList<HomeCircuitReadingImportPreview> ?? previewRows.ToList();
            ImportProgressReporter.Report(progress, 0, rows.Count, stride: 50);

            for (var i = 0; i < rows.Count; i++)
            {
                _energyStore.UpsertHomeCircuitReading(ToReading(rows[i]));
                ImportProgressReporter.Report(progress, i + 1, rows.Count, stride: 50);
            }

            var result = ImportResult.FromDates(rows.Count, rows.Select(row => row.Timestamp));
            if (result.Start is DateTime start && result.End is DateTime end)
            {
                ImportProgressReporter.Report(progress, rows.Count, rows.Count, ImportProgress.AttributionPhase);
                CalculateEvAttribution(start, end);
            }

            ImportProgressReporter.Report(progress, rows.Count, rows.Count);
            return result;
        }

        public void Update(string[] filePaths)
        {
            try
            {
                var preview = LoadReadingsFromFiles(filePaths);
                var result = ImportReadings(preview);
                ReadingCount = result.Count;
                StartDate = result.Start ?? DateTime.MinValue;
                EndDate = result.End ?? DateTime.MinValue;
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ReadingCount = 0;
                StartDate = DateTime.MinValue;
                EndDate = DateTime.MinValue;
                ErrorMessage = ex.Message;
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
                readings = _energyStore.GetHomeCircuitReadings(start, end)
                    .OrderByDescending(r => r.Timestamp)
                    .ToArray();
            }

            return readings;
        }

        private static HomeCircuitReading ToReading(HomeCircuitReadingImportPreview row) =>
            new()
            {
                Timestamp = row.Timestamp,
                KiloWattHours = row.KiloWattHours
            };
    }
}
