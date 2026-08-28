using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms
{
    public partial class FinanceDashboardForm : BaseDashboardForm
    {
        private FinanceDashboardViewModel ViewModel { get; set; } = null!;

        public FinanceDashboardForm()
        {
            InitializeComponent();
        }

        public override void InitializeDashboard()
        {
            ViewModel = new FinanceDashboardViewModel();

            financeGrid.AutoGenerateColumns = true;
            ViewModel.Initialize();
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
