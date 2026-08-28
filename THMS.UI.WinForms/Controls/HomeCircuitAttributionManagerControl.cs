using System.ComponentModel;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class HomeCircuitAttributionManagerControl : UserControl, IDataManagerControl
    {
        private readonly HomeCircuitAttributionOrchestrator _orchestrator = new();
        private BindingList<HomeCircuitAttribution> _attributions;

        public HomeCircuitAttributionManagerControl()
        {
            InitializeComponent();
            gridHomeCircuit.AutoGenerateColumns = false;
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
