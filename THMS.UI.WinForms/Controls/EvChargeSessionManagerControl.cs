using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class EvChargeSessionManagerControl : UserControl, IDataManagerControl
    {
        private readonly EvChargeSessionOrchestrator _orchestrator;

        private BindingList<BaseEvChargeSession> _sessions;

        public EvChargeSessionManagerControl(IVehicleDataStore vehicleStore, IEnergyDataStore energyStore, IFinanceDataStore financeStore)
        {
            InitializeComponent();

            _gridSessions.AutoGenerateColumns = false;
            _orchestrator = new EvChargeSessionOrchestrator(vehicleStore, energyStore, financeStore);
        }

        // ---------------------------------------------------------
        // IDataManagerControl implementation
        // ---------------------------------------------------------
        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _sessions = new BindingList<BaseEvChargeSession>(_orchestrator.GetEvChargeSessions(period).ToList());
            _gridSessions.DataSource = _sessions;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource("Month");
        }
    }
}
