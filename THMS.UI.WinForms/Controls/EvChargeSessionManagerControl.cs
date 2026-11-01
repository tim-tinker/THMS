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
            DataGridViewUtil.EnableDoubleBuffering(_gridSessions);

            _gridSessions.AutoGenerateColumns = false;
            _gridSessions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _gridSessions.CellFormatting += OnCellFormatting;
            _gridSessions.CellDoubleClick += OnCellDoubleClick;
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
                    DataGridViewUtil.SetContentForeColor(e.CellStyle, UnavailableFore);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    e.CellStyle.Font = _unavailableFont;
                    break;
                case EvChargeEnergyCellKind.Estimated:
                    e.CellStyle.BackColor = EstimatedBack;
                    e.CellStyle.SelectionBackColor = EstimatedBack;
                    DataGridViewUtil.SetContentForeColor(e.CellStyle, EstimatedFore);
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
            DataGridViewUtil.FitDisplayedDateAndMoneyColumns(_gridSessions);
        }

        private void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (_gridSessions.Rows[e.RowIndex].DataBoundItem is not BaseEvChargeSession session)
                return;

            var vehicle = _orchestrator.GetEvVehicles().FirstOrDefault(candidate => candidate.Id == session.VehicleId);
            if (vehicle is null)
            {
                MessageBox.Show(
                    "The vehicle for this charging session is no longer available.",
                    "Edit EV Charge Session",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var form = new EvChargeSessionForm(vehicle, session);
            if (form.ShowDialog(FindForm()) != DialogResult.OK || form.SavedSession is null)
                return;

            _orchestrator.Update(form.SavedSession);
            SetGridDataSource(_currentPeriod);
            SelectSession(form.SavedSession.Id);
        }

        private void SelectSession(Guid sessionId)
        {
            for (var i = 0; i < _gridSessions.Rows.Count; i++)
            {
                if (_gridSessions.Rows[i].DataBoundItem is BaseEvChargeSession session && session.Id == sessionId)
                {
                    DataGridViewUtil.SelectCellWhenReady(_gridSessions, i, 0);
                    return;
                }
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource(_currentPeriod);
        }
    }
}
