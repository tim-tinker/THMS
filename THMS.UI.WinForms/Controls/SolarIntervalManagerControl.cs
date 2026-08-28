using System.ComponentModel;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class SolarIntervalManagerControl : UserControl, IDataManagerControl
    {
        private readonly SolarIntervalOrchestrator _orchestrator = new();
        private BindingList<SolarProductionInterval> _intervals;

        public SolarIntervalManagerControl()
        {
            InitializeComponent();
            gridSolarIntervals.AutoGenerateColumns = false;
        }

        // ---------------------------------------------------------
        // IDataManagerControl implementation
        // ---------------------------------------------------------
        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _intervals = new BindingList<SolarProductionInterval>(_orchestrator.GetSolarIntervals(period).ToList());
            gridSolarIntervals.DataSource = _intervals;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource("Month");
        }
    }
}
