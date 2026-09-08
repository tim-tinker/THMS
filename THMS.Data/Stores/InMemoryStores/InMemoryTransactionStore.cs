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
        private readonly List<ExpenseBudgetHistory> _expenseBudgetHistory = new();
        private readonly List<ExpenseCategory> _categories = new();
        private readonly List<CategoryAssignment> _assignments = new();

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

        public void AddRecurringSingle(RecurringSingleTransactionRule rule)
        {
            var existing = _recurringSingles.FirstOrDefault(r => RecurringRulePattern.Matches(r, rule));
            if (existing is not null)
            {
                UpsertExisting(existing, rule, r => Update(_recurringSingles, r));
                return;
            }

            Add(_recurringSingles, rule);
        }
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

        public void AddRecurringTransfer(RecurringTransferRule rule)
        {
            var existing = _recurringTransfers.FirstOrDefault(r => RecurringRulePattern.Matches(r, rule));
            if (existing is not null)
            {
                UpsertExisting(existing, rule, r => Update(_recurringTransfers, r));
                return;
            }

            Add(_recurringTransfers, rule);
        }
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

        public void AddExpenseBudget(ExpenseBudgetRule rule) => Add(_expenseBudgetRules, rule);
        public void UpdateExpenseBudget(ExpenseBudgetRule rule) => Update(_expenseBudgetRules, rule);
        public void DeleteExpenseBudget(Guid id)
        {
            _expenseBudgetRules.RemoveAll(r => r.Id == id);
            _expenseBudgetHistory.RemoveAll(h => h.BudgetRuleId == id);
        }

        public ExpenseBudgetRule? GetExpenseBudget(Guid id) =>
            _expenseBudgetRules.FirstOrDefault(r => r.Id == id);

        public IEnumerable<ExpenseBudgetRule> GetExpenseBudgetsByAccount(Guid accountId) =>
            _expenseBudgetRules.Where(r => r.AccountId == accountId).OrderBy(r => r.BudgetName);

        public void AddExpenseBudgetHistory(ExpenseBudgetHistory history) => Add(_expenseBudgetHistory, history);
        public void UpdateExpenseBudgetHistory(ExpenseBudgetHistory history) => Update(_expenseBudgetHistory, history);

        public ExpenseBudgetHistory? GetExpenseBudgetHistoryById(Guid id) =>
            _expenseBudgetHistory.FirstOrDefault(h => h.Id == id);

        public IEnumerable<ExpenseBudgetHistory> GetExpenseBudgetHistory(Guid budgetRuleId) =>
            _expenseBudgetHistory.Where(h => h.BudgetRuleId == budgetRuleId).OrderByDescending(h => h.PeriodStart);

        public ExpenseBudgetHistory? GetActiveBudgetHistory(Guid budgetRuleId) =>
            _expenseBudgetHistory
                .Where(h => h.BudgetRuleId == budgetRuleId && !h.IsClosed)
                .OrderByDescending(h => h.PeriodStart)
                .FirstOrDefault();

        public IEnumerable<ExpenseBudgetHistory> GetBudgetHistoryForPeriod(
            Guid budgetRuleId,
            DateTime periodStart,
            DateTime periodEnd) =>
            GetExpenseBudgetHistory(budgetRuleId)
                .Where(h => h.PeriodStart <= periodEnd && h.PeriodEnd >= periodStart);

        public void CloseBudgetHistory(Guid historyId)
        {
            var history = GetExpenseBudgetHistoryById(historyId);
            if (history is null)
                return;

            history.IsClosed = true;
            history.RecalculateRemaining();
            Update(_expenseBudgetHistory, history);
        }

        // ------------------------------------------------------------
        // Categories
        // ------------------------------------------------------------

        public void AddCategory(ExpenseCategory category) => Add(_categories, category);
        public void UpdateCategory(ExpenseCategory category) => Update(_categories, category);
        public void DeleteCategory(Guid id) => DeactivateCategory(id);
        public void DeactivateCategory(Guid id)
        {
            var category = GetCategory(id);
            if (category is null)
                return;
            category.IsActive = false;
            Update(_categories, category);
        }

        public void MergeCategories(Guid keepCategoryId, Guid retireCategoryId)
        {
            if (keepCategoryId == retireCategoryId)
                throw new InvalidOperationException("A category cannot be merged into itself.");

            var keep = GetCategory(keepCategoryId)
                ?? throw new InvalidOperationException($"Category {keepCategoryId} was not found.");
            var retire = GetCategory(retireCategoryId)
                ?? throw new InvalidOperationException($"Category {retireCategoryId} was not found.");

            if (keep.ParentCategoryId == retire.Id)
            {
                keep.ParentCategoryId = retire.ParentCategoryId;
                Update(_categories, keep);
            }

            foreach (var child in _categories.Where(c => c.ParentCategoryId == retire.Id && c.Id != keep.Id).ToList())
            {
                child.ParentCategoryId = keep.Id;
                Update(_categories, child);
            }

            Retarget(_posted, keep, retire.Id);
            Retarget(_postedTransfers, keep, retire.Id);
            Retarget(_futureSingles, keep, retire.Id);
            Retarget(_futureTransfers, keep, retire.Id);
            Retarget(_recurringSingles, keep, retire.Id);
            Retarget(_recurringTransfers, keep, retire.Id);

            foreach (var rule in _expenseBudgetRules)
            {
                if (!rule.IncludedCategoryIds.Contains(retire.Id))
                    continue;

                rule.IncludedCategoryIds = rule.IncludedCategoryIds
                    .Select(id => id == retire.Id ? keep.Id : id)
                    .Distinct()
                    .ToList();
            }

            var keepKeys = _assignments
                .Where(a => a.CategoryId == keep.Id)
                .Select(a => a.NormalizedDescription)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var assignment in _assignments.Where(a => a.CategoryId == retire.Id).ToList())
            {
                if (keepKeys.Contains(assignment.NormalizedDescription))
                    _assignments.Remove(assignment);
                else
                    assignment.CategoryId = keep.Id;
            }

            retire.IsActive = false;
            Update(_categories, retire);
        }

        public ExpenseCategory? GetCategory(Guid id) => _categories.FirstOrDefault(c => c.Id == id);

        public IEnumerable<ExpenseCategory> GetAllCategories(bool includeInactive = false) =>
            _categories
                .Where(c => includeInactive || c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

        public IEnumerable<ExpenseCategory> GetCategoryTree(bool includeInactive = true) =>
            GetAllCategories(includeInactive);

        public IEnumerable<ExpenseCategory> GetChildCategories(Guid parentCategoryId) =>
            _categories
                .Where(c => c.IsActive && c.ParentCategoryId == parentCategoryId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

        public CategoryUsage CountCategoryUsage(Guid categoryId)
        {
            return new CategoryUsage
            {
                TransactionCount =
                    _posted.Count(t => t.CategoryId == categoryId) +
                    _postedTransfers.Count(t => t.CategoryId == categoryId) +
                    _futureSingles.Count(t => t.CategoryId == categoryId) +
                    _futureTransfers.Count(t => t.CategoryId == categoryId),
                RecurringRuleCount =
                    _recurringSingles.Count(r => r.CategoryId == categoryId) +
                    _recurringTransfers.Count(r => r.CategoryId == categoryId),
                BudgetRuleCount = _expenseBudgetRules.Count(r => r.IncludedCategoryIds.Contains(categoryId)),
                LearnedMappingCount = _assignments.Count(a => a.CategoryId == categoryId)
            };
        }

        public IEnumerable<ExpenseBudgetRule> GetAllExpenseBudgets() =>
            _expenseBudgetRules.OrderBy(r => r.BudgetName);

        public void EnsureDefaultCategories()
        {
            foreach (var category in DefaultExpenseCategories.All)
            {
                if (_categories.Any(c => c.Id == category.Id))
                    continue;
                _categories.Add(new ExpenseCategory
                {
                    Id = category.Id,
                    Name = category.Name,
                    ParentCategoryId = category.ParentCategoryId,
                    IsActive = true,
                    DisplayOrder = category.DisplayOrder
                });
            }
        }

        public void UpsertAssignment(string normalizedDescription, Guid categoryId)
        {
            var existing = _assignments.FirstOrDefault(a =>
                string.Equals(a.NormalizedDescription, normalizedDescription, StringComparison.Ordinal));
            if (existing is null)
            {
                _assignments.Add(new CategoryAssignment
                {
                    NormalizedDescription = normalizedDescription,
                    CategoryId = categoryId,
                    UpdatedOn = DateTime.UtcNow
                });
                return;
            }

            existing.CategoryId = categoryId;
            existing.UpdatedOn = DateTime.UtcNow;
        }

        public CategoryAssignment? GetAssignment(string normalizedDescription) =>
            _assignments.FirstOrDefault(a =>
                string.Equals(a.NormalizedDescription, normalizedDescription, StringComparison.Ordinal));

        private static void UpsertExisting<T>(T existing, T incoming, Action<T> update)
            where T : BaseDomainModel
        {
            if (existing is RecurringSingleTransactionRule existingSingle &&
                incoming is RecurringSingleTransactionRule incomingSingle)
            {
                if (existingSingle.IsUserCreated && !incomingSingle.IsUserCreated)
                    return;

                incomingSingle.Id = existingSingle.Id;
                incomingSingle.IsUserCreated = existingSingle.IsUserCreated || incomingSingle.IsUserCreated;
                update(incoming);
                return;
            }

            if (existing is RecurringTransferRule existingTransfer &&
                incoming is RecurringTransferRule incomingTransfer)
            {
                if (existingTransfer.IsUserCreated && !incomingTransfer.IsUserCreated)
                    return;

                incomingTransfer.Id = existingTransfer.Id;
                incomingTransfer.IsUserCreated = existingTransfer.IsUserCreated || incomingTransfer.IsUserCreated;
                update(incoming);
            }
        }

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

        private static void Retarget<T>(IEnumerable<T> items, ExpenseCategory keep, Guid retireId)
            where T : BaseTransaction
        {
            foreach (var item in items.Where(t => t.CategoryId == retireId))
                item.ApplyCategory(keep);
        }
    }
}
