using System.ComponentModel;
using THMS.Domain.Finance;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class ElectricContractManagerControl : UserControl, IDataManagerControl
    {
        private readonly ElectricContractOrchestrator _orchestrator = new();
        private string _currentPeriod = "Month";
        private BindingList<ElectricContract> _contracts;

        public ElectricContractManagerControl()
        {
            InitializeComponent();
            _gridContracts.AutoGenerateColumns = false;
            _gridContracts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            ApplyColumnAutoSize();
        }

        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _currentPeriod = period;
            _contracts = new BindingList<ElectricContract>(_orchestrator.GetElectricContracts(period).ToList());
            _gridContracts.DataSource = _contracts;
            ApplyColumnAutoSize();
        }

        private void ApplyColumnAutoSize()
        {
            foreach (DataGridViewColumn column in _gridContracts.Columns)
            {
                if (column == NameColumn)
                    continue;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            NameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            NameColumn.MinimumWidth = Math.Max(NameColumn.Width, 80);
            NameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource(_currentPeriod);
        }

        private void OnVisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
                SetGridDataSource(_currentPeriod);
        }
    }
}
