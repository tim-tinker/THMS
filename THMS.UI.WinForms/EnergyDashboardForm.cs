using System;
using System.Windows.Forms;
using THMS.Domain;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class EnergyDashboardForm : BaseDashboardForm
    {
        private EnergyDashboardViewModel _vm;

        public EnergyDashboardForm()
        {
            InitializeComponent();
        }

        protected override void OnBindViewModel(BaseDashboardViewModel viewModel)
        {
            _vm = viewModel as EnergyDashboardViewModel
                ?? throw new ArgumentException("Invalid ViewModel type");

            energySourceListBox.DataSource = _vm.EnergySources;
            energySourceListBox.DisplayMember = "Name";

            if (energySourceListBox.Items.Count > 0)
                energySourceListBox.SelectedIndex = 0;
        }

        private void EnergySourceListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_vm is null) return;

            _vm.SelectedSource = energySourceListBox.SelectedItem as EnergySource;
            UpdateEnergyDetails();
            UpdateChart();
        }

        public override void OnActivated()
        {
            UpdateEnergyDetails();
            UpdateChart();
        }

        private void UpdateEnergyDetails()
        {
            if (_vm?.SelectedSource == null) return;

            lblSourceName.Text = _vm.SelectedSource.Name;
            lblMonthlyKwh.Text = $"Monthly kWh: {_vm.SelectedSource.MonthlyKwh:N0}";
            lblCostPerKwh.Text = $"Cost per kWh: {_vm.SelectedSource.CostPerKwh:C}";
            lblMonthlyCost.Text = $"Monthly Cost: {_vm.SelectedSource.MonthlyCost:C}";
        }

        private void UpdateChart()
        {
            if (_vm?.SelectedSource == null) return;

            var series = energyChart.Series["MonthlyEnergyCost"];
            series.Points.Clear();

            foreach (var mc in _vm.SelectedSource.MonthlyCosts)
                series.Points.AddXY(mc.Month, mc.Amount);
        }
    }
}
