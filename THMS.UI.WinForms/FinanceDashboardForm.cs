using THMS.Data.Stores;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms
{
    public partial class FinanceDashboardForm : BaseDashboardForm
    {
        private readonly IFinanceDataStore? _financeDataStore;
        private FinanceDashboardViewModel ViewModel { get; set; } = null!;

        /// <summary>Designer only.</summary>
        public FinanceDashboardForm()
        {
            InitializeComponent();
        }

        public FinanceDashboardForm(IFinanceDataStore financeDataStore) : this()
        {
            _financeDataStore = financeDataStore;
        }

        public override void InitializeDashboard()
        {
            ViewModel = _financeDataStore is null
                ? new FinanceDashboardViewModel()
                : new FinanceDashboardViewModel(_financeDataStore);

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
