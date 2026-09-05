using THMS.Data.Stores;
using THMS.Domain.Finance;

namespace THMS.Logic.ViewModels.Finance
{
    public class FinanceDashboardViewModel : BaseDashboardViewModel
    {
        private readonly IFinanceDataStore _store;

        public FinanceDashboardViewModel()
            : this(new DataStoreFactory().GetFinanceStore())
        {
        }

        public FinanceDashboardViewModel(IFinanceDataStore store)
        {
            _store = store;
        }

        public List<FinanceTransaction> Transactions { get; private set; } = new();
        public decimal TotalSpending { get; private set; }
        public decimal TotalIncome { get; private set; }

        public override void Initialize()
        {
            //Transactions = _store.GetAllTransactions().ToList();
            //ComputeTotals();
        }

        public void Refresh()
        {
            //Transactions = _store.GetAllTransactions().ToList();
            //ComputeTotals();
        }

        private void ComputeTotals()
        {
            TotalSpending = Transactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
            TotalIncome = Transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        }
    }
}
