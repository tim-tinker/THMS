using System.ComponentModel;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.Logic.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms.Controls
{
    public partial class EvChargeSessionManagerControl : UserControl, IDataManagerControl
    {
        private readonly EvChargeSessionOrchestrator _orchestrator = new();
        private readonly ContextMenuStrip _sessionMenu;
        private readonly ToolStripMenuItem _deleteSessionItem = new("Delete");
        private BaseEvChargeSession? _menuSession;
        private bool _menuOpenedFromPointer;

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
            _gridSessions.MouseDown += OnGridMouseDown;
            _sessionMenu = new ContextMenuStrip();
            _deleteSessionItem.Click += (_, _) => DeleteMenuSession();
            _sessionMenu.Items.Add(_deleteSessionItem);
            var refit = new ToolStripMenuItem("Refit");
            refit.Click += OnRefitChargeEnergy;
            _sessionMenu.Items.Add(refit);
            _sessionMenu.Opening += (_, _) => PrepareDeleteItem();
            _gridSessions.ContextMenuStrip = _sessionMenu;
            Disposed += (_, _) => _sessionMenu.Dispose();
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
            var cell = _gridSessions.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (_gridSessions.Columns[e.ColumnIndex] == ChargeKhwColumn && session.KwhAddedIsCalculated)
            {
                cell.ToolTipText = "Calculated from start and end SOC. Excluded when refitting.";
                e.CellStyle.BackColor = EstimatedBack;
                e.CellStyle.SelectionBackColor = EstimatedBack;
                DataGridViewUtil.SetContentForeColor(e.CellStyle, EstimatedFore);
                return;
            }

            if (ToEnergyColumn(_gridSessions.Columns[e.ColumnIndex]) is not EvChargeEnergyColumn column)
                return;

            var hint = EvChargeEnergyCellHints.For(session, column);
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

        private void OnGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            _menuOpenedFromPointer = true;
            var hit = _gridSessions.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.RowIndex >= _gridSessions.Rows.Count)
            {
                _menuSession = null;
                return;
            }

            var column = hit.ColumnIndex;
            if (column < 0 || column >= _gridSessions.Columns.Count || !_gridSessions.Columns[column].Visible)
                column = FirstVisibleColumn();
            if (column >= 0)
                _gridSessions.CurrentCell = _gridSessions.Rows[hit.RowIndex].Cells[column];

            _menuSession = _gridSessions.Rows[hit.RowIndex].DataBoundItem as BaseEvChargeSession;
        }

        private int FirstVisibleColumn()
        {
            foreach (DataGridViewColumn column in _gridSessions.Columns)
            {
                if (column.Visible)
                    return column.Index;
            }

            return -1;
        }

        private void PrepareDeleteItem()
        {
            if (!_menuOpenedFromPointer)
                _menuSession = _gridSessions.CurrentRow?.DataBoundItem as BaseEvChargeSession;

            _menuOpenedFromPointer = false;
            _deleteSessionItem.Enabled = _menuSession is not null;
        }

        private void DeleteMenuSession()
        {
            if (_menuSession is not BaseEvChargeSession session)
                return;

            var name = string.IsNullOrWhiteSpace(session.VehicleName) ? "this vehicle" : session.VehicleName.Trim();
            if (MessageBox.Show(
                    FindForm(),
                    $"Delete the {session.StartTime:g} charge session for {name}?",
                    "Delete Charge Session",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                _orchestrator.Delete(session.Id);
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(FindForm(), "Delete Charge Session", ex);
                return;
            }

            SetGridDataSource(_currentPeriod);
        }

        private void OnRefitChargeEnergy(object? sender, EventArgs e)
        {
            var fits = _orchestrator.RecalculateChargeEnergy();
            SetGridDataSource(_currentPeriod);
            MessageBox.Show(
                FindForm(),
                DescribeRefit(fits),
                "Refit",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static string DescribeRefit(IReadOnlyList<(VehicleEv Vehicle, SocEnergyFit Fit)> fits)
        {
            if (fits.Count == 0)
                return "No measured battery kWh values are available to refit.";

            return string.Join(
                Environment.NewLine + Environment.NewLine,
                fits.Select(item => DescribeVehicle(item.Vehicle, item.Fit)));
        }

        private static string DescribeVehicle(VehicleEv vehicle, SocEnergyFit fit)
        {
            var energy = fit.Coefficients;
            var text = $"{vehicle.Name}{Environment.NewLine}{Polynomial(energy)}";
            text += fit.HigherTermsUsed
                ? $"{Environment.NewLine}Fit from {fit.MeasuredSessions} measured sessions, including {fit.ExtremeSessions} that start below 20% or end above 90%."
                : $"{Environment.NewLine}Quadratic and cubic terms stay 0 until {BatterySocEnergy.MinimumExtremeSessions} measured sessions start below 20% or end above 90%. This vehicle has {fit.ExtremeSessions}.";
            if (fit.RefreshedCalculatedSessions > 0)
                text += $"{Environment.NewLine}Updated {Count(fit.RefreshedCalculatedSessions, "calculated session")}.";
            if (fit.UnchangedCalculatedSessions > 0)
                text += $"{Environment.NewLine}{Count(fit.UnchangedCalculatedSessions, "calculated session")} stayed unchanged because end SOC is not above start SOC.";
            return text;
        }

        private static string Count(int count, string noun) =>
            count == 1 ? $"1 {noun}" : $"{count} {noun}s";

        private static string Polynomial(SocEnergyCoefficients energy) =>
            $"E(s) = {Term(energy.A1, "s", true)} {Term(energy.A2, "s²", false)} {Term(energy.A3, "s³", false)}";

        private static string Term(decimal value, string symbol, bool first)
        {
            var magnitude = (value < 0 ? -value : value).ToString("0.########");
            if (value == 0)
                magnitude = "0";
            if (first)
                return $"{(value < 0 ? "-" : "")}{magnitude}·{symbol}";
            return $"{(value < 0 ? "-" : "+")} {magnitude}·{symbol}";
        }
    }
}
