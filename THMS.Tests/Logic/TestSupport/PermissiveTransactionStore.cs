using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Tests.Logic.TestSupport
{
    /// <summary>
    /// In-memory transaction store that upserts on replace so transfer detection
    /// can finish after matching an imported transaction that is not yet persisted.
    /// </summary>
    public sealed class PermissiveTransactionStore : ITransactionDataStore, ICategoryDataStore
    {
        private readonly InMemoryTransactionDataStore _inner = new();

        public void AddPostedTransaction(PostedTransaction transaction) => _inner.AddPostedTransaction(transaction);
        public void UpdatePostedTransaction(PostedTransaction transaction) => _inner.UpdatePostedTransaction(transaction);
        public void DeletePostedTransaction(Guid id) => _inner.DeletePostedTransaction(id);
        public PostedTransaction? GetPostedTransaction(Guid id) => _inner.GetPostedTransaction(id);
        public IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId) => _inner.GetPostedTransactions(accountId);
        public IEnumerable<PostedTransaction> GetPostedTransactions(DateTime start, DateTime end) => _inner.GetPostedTransactions(start, end);
        public IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId, DateTime start, DateTime end) =>
            _inner.GetPostedTransactions(accountId, start, end);
        public decimal SumPostedAmountsBefore(Guid accountId, DateTime before) =>
            _inner.SumPostedAmountsBefore(accountId, before);
        public decimal SumPostedAmountsAfter(Guid accountId, DateTime after) =>
            _inner.SumPostedAmountsAfter(accountId, after);
        public DateTime? GetLatestPostedTransactionDate(Guid accountId) => _inner.GetLatestPostedTransactionDate(accountId);

        public void ReplacePostedTransaction(PostedTransaction replacement)
        {
            try
            {
                _inner.ReplacePostedTransaction(replacement);
            }
            catch (InvalidOperationException)
            {
                _inner.AddPostedTransaction(replacement);
            }
        }

        public void AddPostedTransferTransaction(PostedTransferTransaction transaction) => _inner.AddPostedTransferTransaction(transaction);
        public void UpdatePostedTransferTransaction(PostedTransferTransaction transaction) => _inner.UpdatePostedTransferTransaction(transaction);
        public void DeletePostedTransferTransaction(Guid id) => _inner.DeletePostedTransferTransaction(id);
        public PostedTransferTransaction? GetPostedTransferTransaction(Guid id) => _inner.GetPostedTransferTransaction(id);
        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId) => _inner.GetPostedTransferTransactions(accountId);
        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(DateTime start, DateTime end) => _inner.GetPostedTransferTransactions(start, end);
        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId, DateTime start, DateTime end) =>
            _inner.GetPostedTransferTransactions(accountId, start, end);
        public decimal SumPostedTransferAmountsBefore(Guid accountId, DateTime before) =>
            _inner.SumPostedTransferAmountsBefore(accountId, before);
        public decimal SumPostedTransferAmountsAfter(Guid accountId, DateTime after) =>
            _inner.SumPostedTransferAmountsAfter(accountId, after);
        public DateTime? GetLatestPostedTransferTransactionDate(Guid accountId) => _inner.GetLatestPostedTransferTransactionDate(accountId);

        public void AddFutureSingleTransaction(FutureSingleTransaction transaction) => _inner.AddFutureSingleTransaction(transaction);
        public void UpdateFutureSingleTransaction(FutureSingleTransaction transaction) => _inner.UpdateFutureSingleTransaction(transaction);
        public void DeleteFutureSingleTransaction(Guid id) => _inner.DeleteFutureSingleTransaction(id);
        public FutureSingleTransaction? GetFutureSingleTransaction(Guid id) => _inner.GetFutureSingleTransaction(id);
        public IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(Guid accountId) => _inner.GetFutureSingleTransactions(accountId);
        public IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(DateTime start, DateTime end) => _inner.GetFutureSingleTransactions(start, end);
        public List<FutureSingleTransaction> GetPlannedPayments(Guid accountId) => _inner.GetPlannedPayments(accountId);
        public List<FutureSingleTransaction> GetAllPlannedPayments() => _inner.GetAllPlannedPayments();

        public void AddFutureTransferTransaction(FutureTransferTransaction transaction) => _inner.AddFutureTransferTransaction(transaction);
        public void UpdateFutureTransferTransaction(FutureTransferTransaction transaction) => _inner.UpdateFutureTransferTransaction(transaction);
        public void DeleteFutureTransferTransaction(Guid id) => _inner.DeleteFutureTransferTransaction(id);
        public FutureTransferTransaction? GetFutureTransferTransaction(Guid id) => _inner.GetFutureTransferTransaction(id);
        public IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(Guid accountId) => _inner.GetFutureTransferTransactions(accountId);
        public IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(DateTime start, DateTime end) => _inner.GetFutureTransferTransactions(start, end);
        public List<FutureTransferTransaction> GetPlannedTransfers(Guid accountId) => _inner.GetPlannedTransfers(accountId);
        public List<FutureTransferTransaction> GetAllPlannedTransfers() => _inner.GetAllPlannedTransfers();

        public void AddRecurringSingleRule(RecurringSingleTransactionRule rule) => _inner.AddRecurringSingleRule(rule);
        public void UpdateRecurringSingleRule(RecurringSingleTransactionRule rule) => _inner.UpdateRecurringSingleRule(rule);
        public void DeleteRecurringSingleRule(Guid id) => _inner.DeleteRecurringSingleRule(id);
        public RecurringSingleTransactionRule? GetRecurringSingleRule(Guid id) => _inner.GetRecurringSingleRule(id);
        public IEnumerable<RecurringSingleTransactionRule> GetRecurringSingleRules(Guid accountId) => _inner.GetRecurringSingleRules(accountId);

        public void AddRecurringTransferRule(RecurringTransferRule rule) => _inner.AddRecurringTransferRule(rule);
        public void UpdateRecurringTransferRule(RecurringTransferRule rule) => _inner.UpdateRecurringTransferRule(rule);
        public void DeleteRecurringTransferRule(Guid id) => _inner.DeleteRecurringTransferRule(id);
        public RecurringTransferRule? GetRecurringTransferRule(Guid id) => _inner.GetRecurringTransferRule(id);
        public IEnumerable<RecurringTransferRule> GetRecurringTransferRules(Guid accountId) => _inner.GetRecurringTransferRules(accountId);

        public void AddExpenseBudgetRule(ExpenseBudgetRule rule) => _inner.AddExpenseBudgetRule(rule);
        public void UpdateExpenseBudgetRule(ExpenseBudgetRule rule) => _inner.UpdateExpenseBudgetRule(rule);
        public void DeleteExpenseBudgetRule(Guid ruleId) => _inner.DeleteExpenseBudgetRule(ruleId);
        public ExpenseBudgetRule? GetExpenseBudgetRule(Guid ruleId) => _inner.GetExpenseBudgetRule(ruleId);
        public IEnumerable<ExpenseBudgetRule> GetExpenseBudgetRules(Guid accountId) => _inner.GetExpenseBudgetRules(accountId);

        public void AddExpenseBudgetHistory(ExpenseBudgetHistory history) => _inner.AddExpenseBudgetHistory(history);
        public void UpdateExpenseBudgetHistory(ExpenseBudgetHistory history) => _inner.UpdateExpenseBudgetHistory(history);
        public ExpenseBudgetHistory? GetExpenseBudgetHistoryById(Guid historyId) => _inner.GetExpenseBudgetHistoryById(historyId);
        public IEnumerable<ExpenseBudgetHistory> GetExpenseBudgetHistory(Guid budgetRuleId) => _inner.GetExpenseBudgetHistory(budgetRuleId);
        public ExpenseBudgetHistory? GetActiveBudgetHistory(Guid budgetRuleId) => _inner.GetActiveBudgetHistory(budgetRuleId);
        public IEnumerable<ExpenseBudgetHistory> GetBudgetHistoryForPeriod(Guid budgetRuleId, DateTime periodStart, DateTime periodEnd) =>
            _inner.GetBudgetHistoryForPeriod(budgetRuleId, periodStart, periodEnd);
        public void CloseBudgetHistory(Guid historyId) => _inner.CloseBudgetHistory(historyId);

        public void AddCategory(ExpenseCategory category) => _inner.AddCategory(category);
        public void UpdateCategory(ExpenseCategory category) => _inner.UpdateCategory(category);
        public void DeleteCategory(Guid categoryId) => _inner.DeleteCategory(categoryId);
        public void DeactivateCategory(Guid categoryId) => _inner.DeactivateCategory(categoryId);
        public void MergeCategories(Guid keepCategoryId, Guid retireCategoryId) =>
            _inner.MergeCategories(keepCategoryId, retireCategoryId);
        public ExpenseCategory? GetCategory(Guid categoryId) => _inner.GetCategory(categoryId);
        public IEnumerable<ExpenseCategory> GetAllCategories(bool includeInactive = false) => _inner.GetAllCategories(includeInactive);
        public IEnumerable<ExpenseCategory> GetChildCategories(Guid parentCategoryId) => _inner.GetChildCategories(parentCategoryId);
        public IEnumerable<ExpenseCategory> GetCategoryTree(bool includeInactive = true) => _inner.GetCategoryTree(includeInactive);
        public CategoryUsage CountCategoryUsage(Guid categoryId) => _inner.CountCategoryUsage(categoryId);
        public void EnsureDefaultCategories() => _inner.EnsureDefaultCategories();
        public void UpsertAssignment(string normalizedDescription, Guid categoryId) => _inner.UpsertAssignment(normalizedDescription, categoryId);
        public CategoryAssignment? GetAssignment(string normalizedDescription) => _inner.GetAssignment(normalizedDescription);
        public IEnumerable<ExpenseBudgetRule> GetAllExpenseBudgetRules() => _inner.GetAllExpenseBudgetRules();

        public void SaveSplits(Guid parentId, IEnumerable<SplitTransactionRow> splits) =>
            _inner.SaveSplits(parentId, splits);

        public List<SplitTransactionRow> GetSplits(Guid parentId) => _inner.GetSplits(parentId);
        public void DeleteSplits(Guid parentId) => _inner.DeleteSplits(parentId);

        public IEnumerable<PostedTransaction> GetUnmatchedPostedTransactions(Guid accountId) => _inner.GetUnmatchedPostedTransactions(accountId);
        public IEnumerable<PostedTransferTransaction> GetUnmatchedPostedTransferTransactions(Guid accountId) => _inner.GetUnmatchedPostedTransferTransactions(accountId);
        public IEnumerable<FutureSingleTransaction> GetRealizedFutureSingleTransactions(DateTime cutoff) => _inner.GetRealizedFutureSingleTransactions(cutoff);
        public IEnumerable<FutureTransferTransaction> GetRealizedFutureTransferTransactions(DateTime cutoff) => _inner.GetRealizedFutureTransferTransactions(cutoff);
        public IEnumerable<FutureSingleTransaction> GetAllFutureSingleTransactions() => _inner.GetAllFutureSingleTransactions();
        public IEnumerable<FutureTransferTransaction> GetAllFutureTransferTransactions() => _inner.GetAllFutureTransferTransactions();
        public IEnumerable<RecurringSingleTransactionRule> GetAllRecurringSingleRules() => _inner.GetAllRecurringSingleRules();
        public IEnumerable<RecurringTransferRule> GetAllRecurringTransferRules() => _inner.GetAllRecurringTransferRules();
    }
}
