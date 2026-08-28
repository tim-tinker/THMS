using System.ComponentModel;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class HomeCircuitManagerControl : UserControl, IDataManagerControl
    {
        private readonly HomeCircuitReadingOrchestrator _orchestrator = new();
        private BindingList<HomeCircuitReading> _readings;

        public HomeCircuitManagerControl()
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
            _readings = new BindingList<HomeCircuitReading>(_orchestrator.GetHomeCircuitReadings(period).ToList());
            gridHomeCircuit.DataSource = _readings;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource("Month");
        }
    }
}
