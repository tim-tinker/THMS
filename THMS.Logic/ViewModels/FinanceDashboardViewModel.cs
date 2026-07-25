using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Logic.ViewModels;

namespace THMS.Logic.ViewModels.Finance
{
    public class FinanceDashboardViewModel : BaseDashboardViewModel
    {
        // Lazy dependencies (replace with your real store/engine later)
        private SQLLiteFinanceDataStore? _store;

        protected SQLLiteFinanceDataStore Store =>
            _store ??= new SQLLiteFinanceDataStore();

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
        public void SetStoreForTesting(SQLLiteFinanceDataStore store)
        {
            _store = store;
        }
    }
}
