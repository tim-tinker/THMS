using System;
using System.Windows.Forms;
using THMS.Domain;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class HouseholdDashboardForm : BaseDashboardForm
    {
        private HouseholdDashboardViewModel _vm;

        public HouseholdDashboardForm()
        {
            InitializeComponent();
        }

        protected override void OnBindViewModel(BaseDashboardViewModel viewModel)
        {
            _vm = viewModel as HouseholdDashboardViewModel
                ?? throw new ArgumentException("Invalid ViewModel type");

            expenseListBox.DataSource = _vm.Expenses;
            expenseListBox.DisplayMember = "Name";

            if (expenseListBox.Items.Count > 0)
                expenseListBox.SelectedIndex = 0;
        }

        private void ExpenseListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _vm.SelectedExpense = expenseListBox.SelectedItem as HouseholdExpense;
            UpdateExpenseDetails();
            UpdateChart();
        }

        public override void OnActivated()
        {
            UpdateExpenseDetails();
            UpdateChart();
        }

        private void UpdateExpenseDetails()
        {
            if (_vm?.SelectedExpense == null) return;

            lblExpenseName.Text = _vm.SelectedExpense.Name;
            lblMonthlyCost.Text = $"Monthly Cost: {_vm.SelectedExpense.MonthlyCost:C}";
            lblCategory.Text = $"Category: {_vm.SelectedExpense.Category}";
            lblSharedWith.Text = $"Shared With: {_vm.SelectedExpense.SharedWith}";
        }

        private void UpdateChart()
        {
            if (_vm?.SelectedExpense == null) return;

            var series = householdChart.Series["MonthlyExpense"];
            series.Points.Clear();

            foreach (var mc in _vm.SelectedExpense.MonthlyBreakdown)
                series.Points.AddXY(mc.Month, mc.Amount);
        }
    }
}
