using System.ComponentModel;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class EvChargeSessionManagerControl : UserControl, IDataManagerControl
    {
        private readonly EvChargeSessionOrchestrator _orchestrator = new();

        private BindingList<BaseEvChargeSession> _sessions;
        private string _currentPeriod = "Month";

        public EvChargeSessionManagerControl()
        {
            InitializeComponent();

            _gridSessions.AutoGenerateColumns = false;
            _gridSessions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
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
