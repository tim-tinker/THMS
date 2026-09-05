using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryTransactionStore
    {
        private readonly List<PostedTransaction> _posted = new();
        private readonly List<PostedTransferTransaction> _postedTransfers = new();
        private readonly List<FutureSingleTransaction> _futureSingles = new();
        private readonly List<FutureTransferTransaction> _futureTransfers = new();
        private readonly List<RecurringSingleTransactionRule> _recurringSingles = new();
        private readonly List<RecurringTransferRule> _recurringTransfers = new();
        private readonly List<ExpenseBudgetRule> _expenseBudgetRules = new();
        private readonly List<TransactionCategory> _categories = new();

        // ------------------------------------------------------------
        // Posted Transactions
        // ------------------------------------------------------------

        public void AddPosted(PostedTransaction transaction) => Add(_posted, transaction);
        public void UpdatePosted(PostedTransaction transaction) => Update(_posted, transaction);
        public void DeletePosted(Guid id) => _posted.RemoveAll(t => t.Id == id);
        public PostedTransaction? GetPosted(Guid id) => _posted.FirstOrDefault(t => t.Id == id);

        public IEnumerable<PostedTransaction> GetPostedByAccount(Guid accountId) =>
            _posted.Where(t => t.AccountId == accountId).OrderBy(t => t.Date);

        public IEnumerable<PostedTransaction> GetPostedByDateRange(DateTime start, DateTime end) =>
            _posted.Where(t => InRange(t.Date, start, end)).OrderBy(t => t.Date);

        public PostedTransaction? GetLatestPosted(Guid accountId) =>
            _posted
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Date)
                .FirstOrDefault();

        public IEnumerable<PostedTransaction> GetUnmatchedPosted(Guid accountId)
        {
            var matchedIds = _postedTransfers
                .Select(t => t.RelatedPostedTransactionId)
                .Where(id => id != Guid.Empty)
                .ToHashSet();

            return _posted
                .Where(t => t.AccountId == accountId && !matchedIds.Contains(t.Id))
                .OrderBy(t => t.Date);
        }

        public void ReplacePostedTransaction(PostedTransaction replacement)
        {
            // 1. Find existing transaction by Id
            var existing = _posted
                .FirstOrDefault(t => t.Id == replacement.Id);

            if (existing is null)
                throw new InvalidOperationException(
                    $"Posted transaction {replacement.Id} not found.");

            // 2. Remove the old transaction
            _posted.Remove(existing);

            // 3. Insert the replacement
            _posted.Add(replacement);
        }

        // ------------------------------------------------------------
        // Posted Transfer Transactions
        // ------------------------------------------------------------

        public void AddPostedTransfer(PostedTransferTransaction transaction) => Add(_postedTransfers, transaction);
        public void UpdatePostedTransfer(PostedTransferTransaction transaction) => Update(_postedTransfers, transaction);
        public void DeletePostedTransfer(Guid id) => _postedTransfers.RemoveAll(t => t.Id == id);
        public PostedTransferTransaction? GetPostedTransfer(Guid id) => _postedTransfers.FirstOrDefault(t => t.Id == id);

        public IEnumerable<PostedTransferTransaction> GetPostedTransfersByAccount(Guid accountId) =>
            _postedTransfers.Where(t => t.AccountId == accountId).OrderBy(t => t.Date);

        public IEnumerable<PostedTransferTransaction> GetPostedTransfersByDateRange(DateTime start, DateTime end) =>
            _postedTransfers.Where(t => InRange(t.Date, start, end)).OrderBy(t => t.Date);

        public PostedTransferTransaction? GetLatestPostedTransfer(Guid accountId) =>
            _postedTransfers
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Date)
                .FirstOrDefault();

        public IEnumerable<PostedTransferTransaction> GetUnmatchedPostedTransfers(Guid accountId) =>
            _postedTransfers
                .Where(t => t.AccountId == accountId && t.RelatedPostedTransactionId == Guid.Empty)
                .OrderBy(t => t.Date);

        // ------------------------------------------------------------
        // Future Single Transactions
        // ------------------------------------------------------------

        public void AddFutureSingle(FutureSingleTransaction transaction) => Add(_futureSingles, transaction);
        public void UpdateFutureSingle(FutureSingleTransaction transaction) => Update(_futureSingles, transaction);
        public void DeleteFutureSingle(Guid id) => _futureSingles.RemoveAll(t => t.Id == id);
        public FutureSingleTransaction? GetFutureSingle(Guid id) => _futureSingles.FirstOrDefault(t => t.Id == id);

        public IEnumerable<FutureSingleTransaction> GetFutureSinglesByAccount(Guid accountId) =>
            _futureSingles.Where(t => t.AccountId == accountId).OrderBy(t => t.Date);

        public IEnumerable<FutureSingleTransaction> GetFutureSinglesByDateRange(DateTime start, DateTime end) =>
            _futureSingles.Where(t => InRange(t.Date, start, end)).OrderBy(t => t.Date);

        public IEnumerable<FutureSingleTransaction> GetAllFutureSingles() =>
            _futureSingles.OrderBy(t => t.Date);

        public IEnumerable<FutureSingleTransaction> GetRealizedFutureSingles(DateTime cutoff) =>
            _futureSingles.Where(t => t.IsRealized && t.Date <= cutoff).OrderBy(t => t.Date);

        // ------------------------------------------------------------
        // Future Transfer Transactions
        // ------------------------------------------------------------

        public void AddFutureTransfer(FutureTransferTransaction transaction) => Add(_futureTransfers, transaction);
        public void UpdateFutureTransfer(FutureTransferTransaction transaction) => Update(_futureTransfers, transaction);
        public void DeleteFutureTransfer(Guid id) => _futureTransfers.RemoveAll(t => t.Id == id);
        public FutureTransferTransaction? GetFutureTransfer(Guid id) => _futureTransfers.FirstOrDefault(t => t.Id == id);

        public IEnumerable<FutureTransferTransaction> GetFutureTransfersByAccount(Guid accountId) =>
            _futureTransfers
                .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                .OrderBy(t => t.Date);

        public IEnumerable<FutureTransferTransaction> GetFutureTransfersByDateRange(DateTime start, DateTime end) =>
            _futureTransfers.Where(t => InRange(t.Date, start, end)).OrderBy(t => t.Date);

        public IEnumerable<FutureTransferTransaction> GetAllFutureTransfers() =>
            _futureTransfers.OrderBy(t => t.Date);

        public IEnumerable<FutureTransferTransaction> GetRealizedFutureTransfers(DateTime cutoff) =>
            _futureTransfers.Where(t => t.IsRealized && t.Date <= cutoff).OrderBy(t => t.Date);

        // ------------------------------------------------------------
        // Recurring Single Rules
        // ------------------------------------------------------------

        public void AddRecurringSingle(RecurringSingleTransactionRule rule) => Add(_recurringSingles, rule);
        public void UpdateRecurringSingle(RecurringSingleTransactionRule rule) => Update(_recurringSingles, rule);
        public void DeleteRecurringSingle(Guid id) => _recurringSingles.RemoveAll(r => r.Id == id);
        public RecurringSingleTransactionRule? GetRecurringSingle(Guid id) => _recurringSingles.FirstOrDefault(r => r.Id == id);

        public IEnumerable<RecurringSingleTransactionRule> GetRecurringSinglesByAccount(Guid accountId) =>
            _recurringSingles.Where(r => r.AccountId == accountId).OrderBy(r => r.Date);

        public IEnumerable<RecurringSingleTransactionRule> GetAllRecurringSingles() =>
            _recurringSingles.OrderBy(r => r.Date);

        // ------------------------------------------------------------
        // Recurring Transfer Rules
        // ------------------------------------------------------------

        public void AddRecurringTransfer(RecurringTransferRule rule) => Add(_recurringTransfers, rule);
        public void UpdateRecurringTransfer(RecurringTransferRule rule) => Update(_recurringTransfers, rule);
        public void DeleteRecurringTransfer(Guid id) => _recurringTransfers.RemoveAll(r => r.Id == id);
        public RecurringTransferRule? GetRecurringTransfer(Guid id) => _recurringTransfers.FirstOrDefault(r => r.Id == id);

        public IEnumerable<RecurringTransferRule> GetRecurringTransfersByAccount(Guid accountId) =>
            _recurringTransfers
                .Where(r => r.FromAccountId == accountId || r.ToAccountId == accountId)
                .OrderBy(r => r.Date);

        public IEnumerable<RecurringTransferRule> GetAllRecurringTransfers() =>
            _recurringTransfers.OrderBy(r => r.Date);

        // ------------------------------------------------------------
        // Expense Budget Rules
        // ------------------------------------------------------------

        public void UpsertExpenseBudget(ExpenseBudgetRule? rule)
        {
            if (rule is null)
                return;

            var index = _expenseBudgetRules.FindIndex(r =>
                r.Id == rule.Id ||
                (r.AccountId == rule.AccountId && r.Category == rule.Category));

            if (index < 0)
                _expenseBudgetRules.Add(rule);
            else
                _expenseBudgetRules[index] = rule;
        }

        public IEnumerable<ExpenseBudgetRule> GetExpenseBudgetsByAccount(Guid accountId) =>
            _expenseBudgetRules.Where(r => r.AccountId == accountId);

        // ------------------------------------------------------------
        // Categories
        // ------------------------------------------------------------

        public void AddCategory(TransactionCategory category) => Add(_categories, category);
        public void UpdateCategory(TransactionCategory category) => Update(_categories, category);
        public void DeleteCategory(Guid id) => _categories.RemoveAll(c => c.Id == id);
        public TransactionCategory? GetCategory(Guid id) => _categories.FirstOrDefault(c => c.Id == id);
        public IEnumerable<TransactionCategory> GetAllCategories() => _categories.OrderBy(c => c.Name);

        private static void Add<T>(List<T> items, T item) where T : BaseDomainModel
        {
            if (items.Any(x => x.Id == item.Id))
                throw new InvalidOperationException($"{typeof(T).Name} with Id {item.Id} already exists.");

            items.Add(item);
        }

        private static void Update<T>(List<T> items, T item) where T : BaseDomainModel
        {
            var index = items.FindIndex(x => x.Id == item.Id);
            if (index < 0)
                throw new InvalidOperationException($"{typeof(T).Name} with Id {item.Id} was not found.");

            items[index] = item;
        }

        private static bool InRange(DateTime date, DateTime start, DateTime end) =>
            date >= start && date <= end;
    }
}
