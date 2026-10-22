using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
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
            _budgets = new BudgetOrchestrator(transactions, accounts);
            _txOrchestrator = new TransactionOrchestrator(transactions);
        }

        public FinanceDashboardSnapshot Snapshot { get; private set; } = new();

        public void Use(FinanceDashboardSnapshot snapshot) => Snapshot = snapshot;

        public override void Initialize() => Refresh();

        public void Refresh() => Refresh(DateTime.Today);

        public void Refresh(DateTime asOf) => Snapshot = CreateSnapshot(asOf);

        public FinanceDashboardSnapshot CreateAccountSnapshot(DateTime asOf)
        {
            var accounts = HouseholdAccounts();
            return _composer.Compose(
                asOf.Date,
                accounts,
                [],
                [],
                [],
                new Dictionary<Guid, ExpenseBudgetHistory?>(),
                [],
                [],
                []);
        }

        public FinanceDashboardSnapshot CreateSnapshot(DateTime asOf)
        {
            _transactions.EnsureDefaultCategories();
            var ledger = _budgets.RefreshAllActive();

            var accounts = HouseholdAccounts();
            var accountIds = accounts.Select(a => a.Id).ToHashSet();
            var posted = ledger.Posted
                .Where(transaction => accountIds.Contains(transaction.AccountId))
                .ToList();
            var transfers = ledger.Transfers
                .Where(transaction => accountIds.Contains(transaction.AccountId))
                .ToList();
            var activity = ledger.Lines
                .Where(line => accountIds.Contains(line.AccountId))
                .ToList();

            var rules = _transactions.GetAllExpenseBudgetRules().ToList();
            var periods = rules.ToDictionary(r => r.Id, r => _budgets.GetActivePeriod(r.Id));
            var recurring = _transactions.GetAllRecurringSingleRules()
                .Where(r => accountIds.Contains(r.AccountId))
                .ToList();
            var categories = ledger.Categories;
            var forecast = BuildForecast(asOf.Date, accounts);

            return _composer.Compose(
                asOf.Date,
                accounts,
                posted,
                transfers,
                rules,
                periods,
                recurring,
                forecast,
                categories,
                activity);
        }

        private List<Account> HouseholdAccounts() =>
            _accounts.GetAllAccounts()
                .Where(a => a is not InternalAccount)
                .ToList();

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
