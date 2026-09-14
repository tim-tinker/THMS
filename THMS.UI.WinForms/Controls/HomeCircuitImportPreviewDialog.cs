using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Controls
{
    public sealed class HomeCircuitImportPreviewDialog : ImportPreviewDialog<HomeCircuitReadingImportPreview>
    {
        private readonly HomeCircuitReadingOrchestrator _orchestrator;

        public HomeCircuitImportPreviewDialog(IList<HomeCircuitReadingImportPreview> rows)
            : this(rows, new HomeCircuitReadingOrchestrator())
        {
        }

        public HomeCircuitImportPreviewDialog(
            IList<HomeCircuitReadingImportPreview> rows,
            HomeCircuitReadingOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import Circuit Intervals";
        protected override string Heading => "Circuit Intervals to Import (Preview)";
        protected override string Singular => "circuit interval";
        protected override string Plural => "circuit intervals";

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                DateColumn(nameof(HomeCircuitReadingImportPreview.Timestamp), "Timestamp", "g"),
                NumberColumn(nameof(HomeCircuitReadingImportPreview.KiloWattHours), "Energy (kWh)", "N3"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<HomeCircuitReadingImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportReadings(rows, progress);
    }
}
