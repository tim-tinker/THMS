using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class SolarIntervalManagerControl : UserControl, IDataManagerControl
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly SolarIntervalOrchestrator _orchestrator;
        private BindingList<SolarVendorInterval> _intervals;

        public SolarIntervalManagerControl(IEnergyDataStore energyStore)
        {
            InitializeComponent();
            gridSolarIntervals.AutoGenerateColumns = false;

            _energyStore = energyStore;
            _orchestrator = new SolarIntervalOrchestrator(energyStore);
        }

        // ---------------------------------------------------------
        // IDataManagerControl implementation
        // ---------------------------------------------------------
        public Control GetControl() => this;

        public void LoadData()
        {
            var intervals = _orchestrator.GetSolarIntervals(DateTime.Now.AddMonths(-2), DateTime.Now);
            _intervals = new BindingList<SolarVendorInterval>(intervals.ToList());
            gridSolarIntervals.DataSource = _intervals;
        }

        // ---------------------------------------------------------
        // Event: Delete selected interval
        // ---------------------------------------------------------
        private void OnClickDelete(object sender, EventArgs e)
        {
            if (gridSolarIntervals.SelectedRows.Count == 0)
                return;

            // TODO: orchestrator.DeleteSolarInterval(id)

            gridSolarIntervals.Rows.RemoveAt(gridSolarIntervals.SelectedRows[0].Index);
        }

        // ---------------------------------------------------------
        // Event: Edit selected interval
        // ---------------------------------------------------------
        private void OnClickEdit(object sender, EventArgs e)
        {
            if (gridSolarIntervals.SelectedRows.Count == 0)
                return;

            // TODO: open edit panel or dialog
            MessageBox.Show("Edit interval (not yet implemented).");
        }

        // ---------------------------------------------------------
        // Event: Add new interval
        // ---------------------------------------------------------
        private void OnClickAdd(object sender, EventArgs e)
        {
            // TODO: open add panel or dialog
            MessageBox.Show("Add interval (not yet implemented).");
        }

        private void OnClickMonth(object sender, EventArgs e)
        {
            SetGridDataSource("Month");
        }

        private void OnClickYear(object sender, EventArgs e)
        {
            SetGridDataSource("Year");
        }

        private void OnClickLifetime(object sender, EventArgs e)
        {
            SetGridDataSource("Lifetime");
        }

        private void SetGridDataSource(string interval)
        {
            var latest = _energyStore.GetLatestSolarVendorInterval();
            if (latest is null)
            {
                _intervals = new BindingList<SolarVendorInterval>();
                gridSolarIntervals.DataSource = _intervals;
            }
            else
            {
                var end = latest.Timestamp;
                var start = GetStartDate(end, interval);
                _intervals = new BindingList<SolarVendorInterval>(_orchestrator.GetSolarIntervals(start, end).ToList());
                gridSolarIntervals.DataSource = _intervals;
            }
        }

        private static DateTime GetStartDate(DateTime end, string interval)
        {
            DateTime start = end.AddMonths(-1);
            switch (interval)
            {
                case "Year":
                    start = end.AddYears(-1);
                    break;

                case "Lifetime":
                    start = DateTime.MinValue;
                    break;

                default:
                    start = end.AddMonths(-1);
                    break;
            }

            return start;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource("Month");
        }
    }
}
