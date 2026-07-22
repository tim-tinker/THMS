using System;
using System.Windows.Forms;
using THMS.Domain;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class FinanceDashboardForm : BaseDashboardForm
    {
        private FinanceDashboardViewModel _vm;

        public FinanceDashboardForm()
        {
            InitializeComponent();
        }

        protected override void OnBindViewModel(BaseDashboardViewModel viewModel)
        {
            _vm = viewModel as FinanceDashboardViewModel
                ?? throw new ArgumentException("Invalid ViewModel type");

            accountListBox.DataSource = _vm.Accounts;
            accountListBox.DisplayMember = "Name";

            if (accountListBox.Items.Count > 0)
                accountListBox.SelectedIndex = 0;
        }

        private void AccountListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _vm.SelectedAccount = accountListBox.SelectedItem as FinanceAccount;
            UpdateAccountDetails();
            UpdateChart();
        }

        public override void OnActivated()
        {
            UpdateAccountDetails();
            UpdateChart();
        }

        private void UpdateAccountDetails()
        {
            if (_vm?.SelectedAccount == null) return;

            lblAccountName.Text = _vm.SelectedAccount.Name;
            lblBalance.Text = $"Balance: {_vm.SelectedAccount.Balance:C}";
            lblMonthlyIncome.Text = $"Monthly Income: {_vm.SelectedAccount.MonthlyIncome:C}";
            lblMonthlyExpenses.Text = $"Monthly Expenses: {_vm.SelectedAccount.MonthlyExpenses:C}";
        }

        private void UpdateChart()
        {
            if (_vm?.SelectedAccount == null) return;

            var series = financeChart.Series["MonthlyNet"];
            series.Points.Clear();

            foreach (var mc in _vm.SelectedAccount.MonthlyNet)
                series.Points.AddXY(mc.Month, mc.Amount);
        }
    }
}
