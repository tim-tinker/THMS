using System;
using System.Linq;
using System.Windows.Forms;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms
{
    public partial class FinanceDashboardForm
        : BaseDashboardForm<FinanceDashboardViewModel>
    {
        public FinanceDashboardForm()
        {
            InitializeComponent();
        }

        protected override void BindControlsToViewModel()
        {
            // Grid setup
            financeGrid.AutoGenerateColumns = true;

            // Initial paint
            RefreshDashboard();
        }

        public override void RefreshDashboard()
        {
            ViewModel.Refresh();

            financeGrid.DataSource = ViewModel.Transactions
                .Select(t => new
                {
                    t.Date,
                    t.Description,
                    Amount = t.Amount.ToString("C")
                })
                .ToList();

            lblTotalIncome.Text = $"Income: {ViewModel.TotalIncome:C}";
            lblTotalSpending.Text = $"Spending: {ViewModel.TotalSpending:C}";
        }
    }
}
