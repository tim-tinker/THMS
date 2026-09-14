using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms.Controls
{
    public sealed class EvChargeSessionImportPreviewDialog : ImportPreviewDialog<EvChargeSessionImportPreview>
    {
        private readonly EvChargeSessionImportOrchestrator _orchestrator;

        public EvChargeSessionImportPreviewDialog(IList<EvChargeSessionImportPreview> rows)
            : this(rows, new EvChargeSessionImportOrchestrator())
        {
        }

        public EvChargeSessionImportPreviewDialog(
            IList<EvChargeSessionImportPreview> rows,
            EvChargeSessionImportOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import EV Charge Sessions";
        protected override string Heading => "EV Charge Sessions to Import (Preview)";
        protected override string Singular => "EV charge session";
        protected override string Plural => "EV charge sessions";
        protected override int WindowWidth => 1100;

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                DateColumn(nameof(EvChargeSessionImportPreview.StartTime), "Start", "g"),
                DateColumn(nameof(EvChargeSessionImportPreview.EndTime), "End", "g"),
                TextColumn(nameof(EvChargeSessionImportPreview.VehicleName), "Vehicle", readOnly: true),
                TextColumn(nameof(EvChargeSessionImportPreview.Charger), "Charger", readOnly: true),
                NumberColumn(nameof(EvChargeSessionImportPreview.OdometerMiles), "Odometer", "N1"),
                NumberColumn(nameof(EvChargeSessionImportPreview.StartSoc), "Start SOC", "0'%'"),
                NumberColumn(nameof(EvChargeSessionImportPreview.EndSoc), "End SOC", "0'%'"),
                new DataGridViewCheckBoxColumn
                {
                    DataPropertyName = nameof(EvChargeSessionImportPreview.IsHomeCharge),
                    HeaderText = "Home",
                    Name = nameof(EvChargeSessionImportPreview.IsHomeCharge),
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                },
                NumberColumn(nameof(EvChargeSessionImportPreview.KwhAdded), "Charge kWh", "N2"),
                NumberColumn(nameof(EvChargeSessionImportPreview.KwhDrawn), "Drawn kWh", "N2"),
                NumberColumn(nameof(EvChargeSessionImportPreview.SessionCost), "Cost", "c2"),
                NumberColumn(nameof(EvChargeSessionImportPreview.LastOdometer), "Last Odo", "N1"),
                NumberColumn(nameof(EvChargeSessionImportPreview.LastSoc), "Last SOC", "0'%'"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<EvChargeSessionImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportSessions(rows, progress);
    }
}
