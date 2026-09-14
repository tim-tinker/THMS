using System.ComponentModel;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms.Controls
{
    public partial class EvChargeSessionManagerControl : UserControl, IDataManagerControl
    {
        private readonly EvChargeSessionOrchestrator _orchestrator = new();

        private BindingList<BaseEvChargeSession> _sessions;
        private string _currentPeriod = "Month";
        private readonly Font _unavailableFont;

        public EvChargeSessionManagerControl()
        {
            InitializeComponent();

            _gridSessions.AutoGenerateColumns = false;
            _gridSessions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _gridSessions.CellFormatting += OnCellFormatting;
            _unavailableFont = new Font(_gridSessions.Font, FontStyle.Italic);
            ApplyColumnAutoSize();
        }

        // ---------------------------------------------------------
        // IDataManagerControl implementation
        // ---------------------------------------------------------
        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _currentPeriod = period;
            _sessions = new BindingList<BaseEvChargeSession>(_orchestrator.GetEvChargeSessions(period).ToList());
            _gridSessions.DataSource = _sessions;
            ApplyColumnAutoSize();
        }

        public void SetImportStatus(string message) => lblStatus.Text = message;

        private static readonly Color EstimatedBack = Color.FromArgb(255, 243, 196);
        private static readonly Color EstimatedFore = Color.FromArgb(102, 73, 0);
        private static readonly Color UnavailableFore = Color.FromArgb(110, 110, 110);

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (_gridSessions.Rows[e.RowIndex].DataBoundItem is not BaseEvChargeSession session)
                return;
            if (ToEnergyColumn(_gridSessions.Columns[e.ColumnIndex]) is not EvChargeEnergyColumn column)
                return;

            var hint = EvChargeEnergyCellHints.For(session, column);
            var cell = _gridSessions.Rows[e.RowIndex].Cells[e.ColumnIndex];
            cell.ToolTipText = hint.ToolTip ?? "";

            switch (hint.Kind)
            {
                case EvChargeEnergyCellKind.Unavailable:
                case EvChargeEnergyCellKind.NotApplicable:
                    e.Value = "N/A";
                    e.FormattingApplied = true;
                    e.CellStyle.ForeColor = UnavailableFore;
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    e.CellStyle.Font = _unavailableFont;
                    break;
                case EvChargeEnergyCellKind.Estimated:
                    e.CellStyle.BackColor = EstimatedBack;
                    e.CellStyle.ForeColor = EstimatedFore;
                    break;
            }
        }

        private EvChargeEnergyColumn? ToEnergyColumn(DataGridViewColumn column)
        {
            if (column == EnergyDrawKwhColumn)
                return EvChargeEnergyColumn.Drawn;
            if (column == SolarColumn)
                return EvChargeEnergyColumn.Solar;
            if (column == BatteryColumn)
                return EvChargeEnergyColumn.Battery;
            if (column == GridColumn)
                return EvChargeEnergyColumn.Grid;
            if (column == CostColumn)
                return EvChargeEnergyColumn.Cost;
            return null;
        }

        private void ApplyColumnAutoSize()
        {
            foreach (DataGridViewColumn column in _gridSessions.Columns)
            {
                if (column == VehicleColumn)
                    continue;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            VehicleColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            VehicleColumn.MinimumWidth = Math.Max(VehicleColumn.Width, 80);
            VehicleColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource(_currentPeriod);
        }
    }
}
