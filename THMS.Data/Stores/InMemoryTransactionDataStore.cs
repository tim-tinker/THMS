using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores
{
    public class InMemoryTransactionDataStore : ITransactionDataStore, ICategoryDataStore
    {
        private readonly InMemoryTransactionStore _store = new();

        public InMemoryTransactionDataStore()
        {
            _store.EnsureDefaultCategories();
        }

        // ------------------------------------------------------------
        // Posted Transactions
        // ------------------------------------------------------------

        public void AddPostedTransaction(PostedTransaction transaction) =>
            _store.AddPosted(transaction);

        public void UpdatePostedTransaction(PostedTransaction transaction) =>
            _store.UpdatePosted(transaction);

        public void DeletePostedTransaction(Guid id) =>
            _store.DeletePosted(id);

        public PostedTransaction? GetPostedTransaction(Guid id) =>
            _store.GetPosted(id);

        public IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId) =>
            _store.GetPostedByAccount(accountId);

        public IEnumerable<PostedTransaction> GetPostedTransactions(DateTime start, DateTime end) =>
            _store.GetPostedByDateRange(start, end);

        public DateTime? GetLatestPostedTransactionDate(Guid accountId) =>
            _store.GetLatestPosted(accountId)?.Date;

        public void ReplacePostedTransaction(PostedTransaction replacement) => _store.ReplacePostedTransaction(replacement);

        // ------------------------------------------------------------
        // Posted Transfer Transactions
        // ------------------------------------------------------------

        public void AddPostedTransferTransaction(PostedTransferTransaction transaction) =>
            _store.AddPostedTransfer(transaction);

        public void UpdatePostedTransferTransaction(PostedTransferTransaction transaction) =>
            _store.UpdatePostedTransfer(transaction);

        public void DeletePostedTransferTransaction(Guid id) =>
            _store.DeletePostedTransfer(id);

        public PostedTransferTransaction? GetPostedTransferTransaction(Guid id) =>
            _store.GetPostedTransfer(id);

        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId) =>
            _store.GetPostedTransfersByAccount(accountId);

        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(DateTime start, DateTime end) =>
            _store.GetPostedTransfersByDateRange(start, end);

        public DateTime? GetLatestPostedTransferTransactionDate(Guid accountId) =>
            _store.GetLatestPostedTransfer(accountId)?.Date;

        // ------------------------------------------------------------
        // Future Single Transactions
        // ------------------------------------------------------------

        public void AddFutureSingleTransaction(FutureSingleTransaction transaction) =>
            _store.AddFutureSingle(transaction);

        public void UpdateFutureSingleTransaction(FutureSingleTransaction transaction) =>
            _store.UpdateFutureSingle(transaction);

        public void DeleteFutureSingleTransaction(Guid id) =>
            _store.DeleteFutureSingle(id);

        public FutureSingleTransaction? GetFutureSingleTransaction(Guid id) =>
            _store.GetFutureSingle(id);

        public IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(Guid accountId) =>
            _store.GetFutureSinglesByAccount(accountId);

        public IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(DateTime start, DateTime end) =>
            _store.GetFutureSinglesByDateRange(start, end);

        // ------------------------------------------------------------
        // Future Transfer Transactions
        // ------------------------------------------------------------

        public void AddFutureTransferTransaction(FutureTransferTransaction transaction) =>
            _store.AddFutureTransfer(transaction);

        public void UpdateFutureTransferTransaction(FutureTransferTransaction transaction) =>
            _store.UpdateFutureTransfer(transaction);

        public void DeleteFutureTransferTransaction(Guid id) =>
            _store.DeleteFutureTransfer(id);

        public FutureTransferTransaction? GetFutureTransferTransaction(Guid id) =>
            _store.GetFutureTransfer(id);

        public IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(Guid accountId) =>
            _store.GetFutureTransfersByAccount(accountId);

        public IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(DateTime start, DateTime end) =>
            _store.GetFutureTransfersByDateRange(start, end);

        // ------------------------------------------------------------
        // Recurring Single Rules
        // ------------------------------------------------------------

        public void AddRecurringSingleRule(RecurringSingleTransactionRule rule) =>
            _store.AddRecurringSingle(rule);

        public void UpdateRecurringSingleRule(RecurringSingleTransactionRule rule) =>
            _store.UpdateRecurringSingle(rule);

        public void DeleteRecurringSingleRule(Guid id) =>
            _store.DeleteRecurringSingle(id);

        public RecurringSingleTransactionRule? GetRecurringSingleRule(Guid id) =>
            _store.GetRecurringSingle(id);

        public IEnumerable<RecurringSingleTransactionRule> GetRecurringSingleRules(Guid accountId) =>
            _store.GetRecurringSinglesByAccount(accountId);

        // ------------------------------------------------------------
        // Recurring Transfer Rules
        // ------------------------------------------------------------

        public void AddRecurringTransferRule(RecurringTransferRule rule) =>
            _store.AddRecurringTransfer(rule);

        public void UpdateRecurringTransferRule(RecurringTransferRule rule) =>
            _store.UpdateRecurringTransfer(rule);

        public void DeleteRecurringTransferRule(Guid id) =>
            _store.DeleteRecurringTransfer(id);

        public RecurringTransferRule? GetRecurringTransferRule(Guid id) =>
            _store.GetRecurringTransfer(id);

        public IEnumerable<RecurringTransferRule> GetRecurringTransferRules(Guid accountId) =>
            _store.GetRecurringTransfersByAccount(accountId);

        // ------------------------------------------------------------
        // Expense Budget Rules
        // ------------------------------------------------------------

        public void AddExpenseBudgetRule(ExpenseBudgetRule rule) =>
            _store.AddExpenseBudget(rule);

        public void UpdateExpenseBudgetRule(ExpenseBudgetRule rule) =>
            _store.UpdateExpenseBudget(rule);

        public void DeleteExpenseBudgetRule(Guid ruleId) =>
            _store.DeleteExpenseBudget(ruleId);

        public ExpenseBudgetRule? GetExpenseBudgetRule(Guid ruleId) =>
            _store.GetExpenseBudget(ruleId);

        public IEnumerable<ExpenseBudgetRule> GetExpenseBudgetRules(Guid accountId) =>
            _store.GetExpenseBudgetsByAccount(accountId);

        public void AddExpenseBudgetHistory(ExpenseBudgetHistory history) =>
            _store.AddExpenseBudgetHistory(history);

        public void UpdateExpenseBudgetHistory(ExpenseBudgetHistory history) =>
            _store.UpdateExpenseBudgetHistory(history);

        public ExpenseBudgetHistory? GetExpenseBudgetHistoryById(Guid historyId) =>
            _store.GetExpenseBudgetHistoryById(historyId);

        public IEnumerable<ExpenseBudgetHistory> GetExpenseBudgetHistory(Guid budgetRuleId) =>
            _store.GetExpenseBudgetHistory(budgetRuleId);

        public ExpenseBudgetHistory? GetActiveBudgetHistory(Guid budgetRuleId) =>
            _store.GetActiveBudgetHistory(budgetRuleId);

        public IEnumerable<ExpenseBudgetHistory> GetBudgetHistoryForPeriod(
            Guid budgetRuleId,
            DateTime periodStart,
            DateTime periodEnd) =>
            _store.GetBudgetHistoryForPeriod(budgetRuleId, periodStart, periodEnd);

        public void CloseBudgetHistory(Guid historyId) =>
            _store.CloseBudgetHistory(historyId);

        // ------------------------------------------------------------
        // Categories
        // ------------------------------------------------------------

        public void AddCategory(ExpenseCategory category) =>
            _store.AddCategory(category);

        public void UpdateCategory(ExpenseCategory category) =>
            _store.UpdateCategory(category);

        public void DeleteCategory(Guid categoryId) =>
            _store.DeleteCategory(categoryId);

        public void DeactivateCategory(Guid categoryId) =>
            _store.DeactivateCategory(categoryId);

        public void MergeCategories(Guid keepCategoryId, Guid retireCategoryId) =>
            _store.MergeCategories(keepCategoryId, retireCategoryId);

        public ExpenseCategory? GetCategory(Guid categoryId) =>
            _store.GetCategory(categoryId);

        public IEnumerable<ExpenseCategory> GetAllCategories(bool includeInactive = false) =>
            _store.GetAllCategories(includeInactive);

        public IEnumerable<ExpenseCategory> GetChildCategories(Guid parentCategoryId) =>
            _store.GetChildCategories(parentCategoryId);

        public IEnumerable<ExpenseCategory> GetCategoryTree(bool includeInactive = true) =>
            _store.GetCategoryTree(includeInactive);

        public CategoryUsage CountCategoryUsage(Guid categoryId) =>
            _store.CountCategoryUsage(categoryId);

        public void EnsureDefaultCategories() =>
            _store.EnsureDefaultCategories();

        public void UpsertAssignment(string normalizedDescription, Guid categoryId) =>
            _store.UpsertAssignment(normalizedDescription, categoryId);

        public CategoryAssignment? GetAssignment(string normalizedDescription) =>
            _store.GetAssignment(normalizedDescription);

        public IEnumerable<ExpenseBudgetRule> GetAllExpenseBudgetRules() =>
            _store.GetAllExpenseBudgets();

        // ------------------------------------------------------------
        // Utility Queries
        // ------------------------------------------------------------

        public IEnumerable<PostedTransaction> GetUnmatchedPostedTransactions(Guid accountId) =>
            _store.GetUnmatchedPosted(accountId);

        public IEnumerable<PostedTransferTransaction> GetUnmatchedPostedTransferTransactions(Guid accountId) =>
            _store.GetUnmatchedPostedTransfers(accountId);

        public IEnumerable<FutureSingleTransaction> GetRealizedFutureSingleTransactions(DateTime cutoff) =>
            _store.GetRealizedFutureSingles(cutoff);

        public IEnumerable<FutureTransferTransaction> GetRealizedFutureTransferTransactions(DateTime cutoff) =>
            _store.GetRealizedFutureTransfers(cutoff);

        public IEnumerable<FutureSingleTransaction> GetAllFutureSingleTransactions() =>
            _store.GetAllFutureSingles();

        public IEnumerable<FutureTransferTransaction> GetAllFutureTransferTransactions() =>
            _store.GetAllFutureTransfers();

        public IEnumerable<RecurringSingleTransactionRule> GetAllRecurringSingleRules() =>
            _store.GetAllRecurringSingles();

        public IEnumerable<RecurringTransferRule> GetAllRecurringTransferRules() =>
            _store.GetAllRecurringTransfers();
    }
}
