using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class HomeCircuitManagerControl : UserControl, IDataManagerControl
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly HomeCircuitReadingOrchestrator _orchestrator;
        private BindingList<HomeCircuitReading> _readings;

        public HomeCircuitManagerControl(IEnergyDataStore energyStore)
        {
            InitializeComponent();
            gridHomeCircuit.AutoGenerateColumns = false;

            _energyStore = energyStore;
            _orchestrator = new HomeCircuitReadingOrchestrator(energyStore);
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
