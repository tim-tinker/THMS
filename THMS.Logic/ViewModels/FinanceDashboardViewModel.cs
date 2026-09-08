using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Aggregation;
using THMS.Logic.Orchestrators;

namespace THMS.Logic.ViewModels.Finance
{
    public class FinanceDashboardViewModel : BaseDashboardViewModel
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly BudgetOrchestrator _budgets;
        private readonly TransactionOrchestrator _txOrchestrator;
        private readonly FinanceDashboardComposer _composer = new();

        public FinanceDashboardViewModel()
            : this(new DataStoreFactory().GetAccountStore(), new DataStoreFactory().GetTransactionStore())
        {
        }

        public FinanceDashboardViewModel(IAccountDataStore accounts, ITransactionDataStore transactions)
        {
            _accounts = accounts;
            _transactions = transactions;
            _budgets = new BudgetOrchestrator(transactions);
            _txOrchestrator = new TransactionOrchestrator(transactions);
        }

        public FinanceDashboardSnapshot Snapshot { get; private set; } = new();

        public override void Initialize() => Refresh();

        public void Refresh() => Refresh(DateTime.Today);

        public void Refresh(DateTime asOf)
        {
            _transactions.EnsureDefaultCategories();
            _budgets.RefreshAllActive();

            var accounts = _accounts.GetAllAccounts()
                .Where(a => a is not InternalAccount)
                .ToList();
            var accountIds = accounts.Select(a => a.Id).ToHashSet();

            var posted = new List<PostedTransaction>();
            var transfers = new List<PostedTransferTransaction>();
            foreach (var account in accounts)
            {
                posted.AddRange(_transactions.GetPostedTransactions(account.Id));
                transfers.AddRange(_transactions.GetPostedTransferTransactions(account.Id));
            }

            var rules = _transactions.GetAllExpenseBudgetRules()
                .Where(r => accountIds.Contains(r.AccountId))
                .ToList();
            var periods = rules.ToDictionary(r => r.Id, r => _budgets.GetActivePeriod(r.Id));
            var recurring = _transactions.GetAllRecurringSingleRules()
                .Where(r => accountIds.Contains(r.AccountId))
                .ToList();
            var categories = _transactions.GetAllCategories(includeInactive: true).ToList();
            var forecast = BuildForecast(asOf.Date, accounts);

            Snapshot = _composer.Compose(
                asOf.Date,
                accounts,
                posted,
                transfers,
                rules,
                periods,
                recurring,
                forecast,
                categories);
        }

        private List<UnifiedTransactionView> BuildForecast(DateTime from, List<Account> accounts)
        {
            var to = from.AddDays(90);
            return accounts
                .SelectMany(account => _txOrchestrator.GenerateForecast(account.Id, from, to))
                .GroupBy(f => (f.Date.Date, f.Description, Amount: Math.Abs(f.Amount), f.Type.Contains("Transfer")))
                .Select(g => g.First())
                .OrderBy(f => f.Date)
                .ThenBy(f => f.Description)
                .ToList();
        }
    }
}
