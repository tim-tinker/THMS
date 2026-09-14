using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Ingestion.Importers.Energy;
using THMS.Logic.Energy;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Energy;

namespace THMS.Logic.Orchestrators
{
    public class SolarIntervalOrchestrator : BaseOrchestrator
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly EnphaseSolarImporter _importer = new();

        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int IntervalCount { get; private set; }
        public string ErrorMessage { get; private set; } = "";

        public SolarIntervalOrchestrator()
            : this(new DataStoreFactory().GetEnergyStore())
        {
        }

        public SolarIntervalOrchestrator(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public List<SolarIntervalImportPreview> LoadIntervalsFromFiles(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            var rows = new List<SolarIntervalImportPreview>();
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("A file path is required.");
                if (!File.Exists(path))
                    throw new FileNotFoundException("The selected file was not found.", path);

                foreach (var interval in _importer.Parse(path))
                {
                    rows.Add(new SolarIntervalImportPreview
                    {
                        Timestamp = interval.Timestamp,
                        EnergyProducedWh = interval.EnergyProducedWh,
                        EnergyConsumedWh = interval.EnergyConsumedWh,
                        ExportedToGridWh = interval.ExportedToGridWh,
                        ImportedFromGridWh = interval.ImportedFromGridWh,
                        StoredInBatteriesWh = interval.StoredInBatteriesWh,
                        DischargedFromBatteriesWh = interval.DischargedFromBatteriesWh
                    });
                }
            }

            return rows.OrderBy(row => row.Timestamp).ToList();
        }

        public ImportResult ImportIntervals(IEnumerable<SolarIntervalImportPreview> previewRows) =>
            ImportIntervals(previewRows, progress: null);

        public ImportResult ImportIntervals(
            IEnumerable<SolarIntervalImportPreview> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows as IReadOnlyList<SolarIntervalImportPreview> ?? previewRows.ToList();
            ImportProgressReporter.Report(progress, 0, rows.Count, stride: 50);

            for (var i = 0; i < rows.Count; i++)
            {
                _energyStore.UpsertSolarProductionInterval(ToInterval(rows[i]));
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
                var preview = LoadIntervalsFromFiles(filePaths);
                var result = ImportIntervals(preview);
                IntervalCount = result.Count;
                StartDate = result.Start ?? DateTime.MinValue;
                EndDate = result.End ?? DateTime.MinValue;
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                IntervalCount = 0;
                StartDate = DateTime.MinValue;
                EndDate = DateTime.MinValue;
                ErrorMessage = ex.Message;
            }
        }

        private void CalculateEvAttribution(DateTime start, DateTime end)
        {
            if (_energyStore.GetHomeCircuitReadings(start, end).Any())
            {
                var engine = new HomeCircuitAttributionEngine(_energyStore);
                engine.Compute(start, end);

                foreach (var result in engine.Results)
                {
                    _energyStore.UpsertHomeCircuitAttribution(result);
                }
            }
        }

        public IEnumerable<SolarProductionInterval> GetSolarIntervals(string period)
        {
            var intervals = Array.Empty<SolarProductionInterval>();
            var latest = _energyStore.GetLatestSolarProductionInterval();
            if (latest is not null)
            {
                var end = latest.Timestamp;
                var start = GetStartDate(end, period);
                intervals = _energyStore.GetSolarProductionIntervals(start, end)
                    .OrderByDescending(i => i.Timestamp)
                    .ToArray();
            }

            return intervals;
        }

        private static SolarProductionInterval ToInterval(SolarIntervalImportPreview row) =>
            new()
            {
                Timestamp = row.Timestamp,
                EnergyProducedWh = row.EnergyProducedWh,
                EnergyConsumedWh = row.EnergyConsumedWh,
                ExportedToGridWh = row.ExportedToGridWh,
                ImportedFromGridWh = row.ImportedFromGridWh,
                StoredInBatteriesWh = row.StoredInBatteriesWh,
                DischargedFromBatteriesWh = row.DischargedFromBatteriesWh
            };
    }
}
