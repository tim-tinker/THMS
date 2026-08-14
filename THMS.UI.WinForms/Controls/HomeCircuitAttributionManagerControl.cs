using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class HomeCircuitAttributionManagerControl : UserControl, IDataManagerControl
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly HomeCircuitAttributionOrchestrator _orchestrator;
        private BindingList<HomeCircuitAttribution> _attributions;

        public HomeCircuitAttributionManagerControl(IEnergyDataStore energyStore)
        {
            InitializeComponent();
            gridHomeCircuit.AutoGenerateColumns = false;

            _energyStore = energyStore;
            _orchestrator = new HomeCircuitAttributionOrchestrator(energyStore);
        }

        // ---------------------------------------------------------
        // IDataManagerControl implementation
        // ---------------------------------------------------------
        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _attributions = new BindingList<HomeCircuitAttribution>(_orchestrator.GetHomeCircuitAttributions(period).ToList());
            gridHomeCircuit.DataSource = _attributions;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource("Month");
        }
    }
}
