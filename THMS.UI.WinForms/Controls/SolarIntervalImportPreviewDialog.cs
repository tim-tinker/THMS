using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Controls
{
    public sealed class SolarIntervalImportPreviewDialog : ImportPreviewDialog<SolarIntervalImportPreview>
    {
        private readonly SolarIntervalOrchestrator _orchestrator;

        public SolarIntervalImportPreviewDialog(IList<SolarIntervalImportPreview> rows)
            : this(rows, new SolarIntervalOrchestrator())
        {
        }

        public SolarIntervalImportPreviewDialog(
            IList<SolarIntervalImportPreview> rows,
            SolarIntervalOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import Solar Intervals";
        protected override string Heading => "Solar Intervals to Import (Preview)";
        protected override string Singular => "solar interval";
        protected override string Plural => "solar intervals";
        protected override int WindowWidth => 1100;

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                DateColumn(nameof(SolarIntervalImportPreview.Timestamp), "Timestamp", "g"),
                NumberColumn(nameof(SolarIntervalImportPreview.EnergyProducedWh), "Produced", "N0"),
                NumberColumn(nameof(SolarIntervalImportPreview.EnergyConsumedWh), "Consumed", "N0"),
                NumberColumn(nameof(SolarIntervalImportPreview.ExportedToGridWh), "Exported", "N0"),
                NumberColumn(nameof(SolarIntervalImportPreview.ImportedFromGridWh), "Imported", "N0"),
                NumberColumn(nameof(SolarIntervalImportPreview.StoredInBatteriesWh), "Stored", "N0"),
                NumberColumn(nameof(SolarIntervalImportPreview.DischargedFromBatteriesWh), "Discharged", "N0"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<SolarIntervalImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportIntervals(rows, progress);
    }
}
