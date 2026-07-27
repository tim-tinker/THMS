using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Data.Stores.InMemory;
using THMS.Domain.Finance;
using THMS.Logic.ViewModels;

namespace THMS.Logic.ViewModels.Finance
{
    public class FinanceDashboardViewModel : BaseDashboardViewModel
    {
        // Lazy dependencies (replace with your real store/engine later)
        private IFinanceDataStore? _store;

        protected IFinanceDataStore Store =>
            _store ??= new InMemoryFinanceDataStore();

        public List<FinanceTransaction> Transactions { get; private set; } = new();
        public decimal TotalSpending { get; private set; }
        public decimal TotalIncome { get; private set; }

        public override void Initialize()
        {
            // Load initial data
            Transactions = Store.GetAllTransactions().ToList();
            ComputeTotals();
        }

        public void Refresh()
        {
            Transactions = Store.GetAllTransactions().ToList();
            ComputeTotals();
        }

        private void ComputeTotals()
        {
            TotalSpending = Transactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
            TotalIncome = Transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        }

        // Testing support
        public void SetStoreForTesting(IFinanceDataStore store)
        {
            _store = store;
        }
    }
}
