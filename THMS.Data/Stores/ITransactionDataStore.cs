using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores
{
    public interface ITransactionDataStore
    {
        // ------------------------------------------------------------
        // Posted Transactions (single-account)
        // ------------------------------------------------------------
        void AddPostedTransaction(PostedTransaction transaction);
        void UpdatePostedTransaction(PostedTransaction transaction);
        void ReplacePostedTransaction(PostedTransaction replacement);
        void DeletePostedTransaction(Guid id);

        PostedTransaction? GetPostedTransaction(Guid id);
        IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId);
        IEnumerable<PostedTransaction> GetPostedTransactions(DateTime start, DateTime end);
        IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId, DateTime start, DateTime end);
        decimal SumPostedAmountsBefore(Guid accountId, DateTime before);
        decimal SumPostedAmountsAfter(Guid accountId, DateTime after);
        DateTime? GetLatestPostedTransactionDate(Guid accountId);


        // ------------------------------------------------------------
        // Posted Transfer Transactions (ledger-level)
        // ------------------------------------------------------------
        void AddPostedTransferTransaction(PostedTransferTransaction transaction);
        void UpdatePostedTransferTransaction(PostedTransferTransaction transaction);
        void DeletePostedTransferTransaction(Guid id);

        PostedTransferTransaction? GetPostedTransferTransaction(Guid id);
        IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId);
        IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(DateTime start, DateTime end);
        IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId, DateTime start, DateTime end);
        decimal SumPostedTransferAmountsBefore(Guid accountId, DateTime before);
        decimal SumPostedTransferAmountsAfter(Guid accountId, DateTime after);
        DateTime? GetLatestPostedTransferTransactionDate(Guid accountId);


        // ------------------------------------------------------------
        // Future Single Transactions
        // ------------------------------------------------------------
        void AddFutureSingleTransaction(FutureSingleTransaction transaction);
        void UpdateFutureSingleTransaction(FutureSingleTransaction transaction);
        void DeleteFutureSingleTransaction(Guid id);

        FutureSingleTransaction? GetFutureSingleTransaction(Guid id);
        IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(Guid accountId);
        IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(DateTime start, DateTime end);
        List<FutureSingleTransaction> GetPlannedPayments(Guid accountId);
        List<FutureSingleTransaction> GetAllPlannedPayments();


        // ------------------------------------------------------------
        // Future Transfer Transactions
        // ------------------------------------------------------------
        void AddFutureTransferTransaction(FutureTransferTransaction transaction);
        void UpdateFutureTransferTransaction(FutureTransferTransaction transaction);
        void DeleteFutureTransferTransaction(Guid id);

        FutureTransferTransaction? GetFutureTransferTransaction(Guid id);
        IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(Guid accountId);
        IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(DateTime start, DateTime end);
        List<FutureTransferTransaction> GetPlannedTransfers(Guid accountId);
        List<FutureTransferTransaction> GetAllPlannedTransfers();


        // ------------------------------------------------------------
        // Recurring Single Transaction Rules
        // ------------------------------------------------------------
        void AddRecurringSingleRule(RecurringSingleTransactionRule rule);
        void UpdateRecurringSingleRule(RecurringSingleTransactionRule rule);
        void DeleteRecurringSingleRule(Guid id);

        RecurringSingleTransactionRule? GetRecurringSingleRule(Guid id);
        IEnumerable<RecurringSingleTransactionRule> GetRecurringSingleRules(Guid accountId);


        // ------------------------------------------------------------
        // Recurring Transfer Rules
        // ------------------------------------------------------------
        void AddRecurringTransferRule(RecurringTransferRule rule);
        void UpdateRecurringTransferRule(RecurringTransferRule rule);
        void DeleteRecurringTransferRule(Guid id);

        RecurringTransferRule? GetRecurringTransferRule(Guid id);
        IEnumerable<RecurringTransferRule> GetRecurringTransferRules(Guid accountId);


        // ------------------------------------------------------------
        // Expense Budget Rules
        // ------------------------------------------------------------
        void AddExpenseBudgetRule(ExpenseBudgetRule rule);
        void UpdateExpenseBudgetRule(ExpenseBudgetRule rule);
        void DeleteExpenseBudgetRule(Guid ruleId);
        ExpenseBudgetRule? GetExpenseBudgetRule(Guid ruleId);
        IEnumerable<ExpenseBudgetRule> GetExpenseBudgetRules(Guid accountId);

        // ------------------------------------------------------------
        // Expense Budget History
        // ------------------------------------------------------------
        void AddExpenseBudgetHistory(ExpenseBudgetHistory history);
        void UpdateExpenseBudgetHistory(ExpenseBudgetHistory history);
        ExpenseBudgetHistory? GetExpenseBudgetHistoryById(Guid historyId);
        IEnumerable<ExpenseBudgetHistory> GetExpenseBudgetHistory(Guid budgetRuleId);
        ExpenseBudgetHistory? GetActiveBudgetHistory(Guid budgetRuleId);
        IEnumerable<ExpenseBudgetHistory> GetBudgetHistoryForPeriod(Guid budgetRuleId, DateTime periodStart, DateTime periodEnd);
        void CloseBudgetHistory(Guid historyId);


        // ------------------------------------------------------------
        // Expense Categories
        // ------------------------------------------------------------
        void AddCategory(ExpenseCategory category);
        void UpdateCategory(ExpenseCategory category);
        void DeleteCategory(Guid categoryId);
        void DeactivateCategory(Guid categoryId);
        void MergeCategories(Guid keepCategoryId, Guid retireCategoryId);
        ExpenseCategory? GetCategory(Guid categoryId);
        IEnumerable<ExpenseCategory> GetAllCategories(bool includeInactive = false);
        IEnumerable<ExpenseCategory> GetChildCategories(Guid parentCategoryId);
        IEnumerable<ExpenseCategory> GetCategoryTree(bool includeInactive = true);
        CategoryUsage CountCategoryUsage(Guid categoryId);
        void EnsureDefaultCategories();
        void UpsertAssignment(string normalizedDescription, Guid categoryId);
        CategoryAssignment? GetAssignment(string normalizedDescription);


        // ------------------------------------------------------------
        // Utility Queries (used by Orchestrators)
        // ------------------------------------------------------------

        // For transfer matching
        IEnumerable<PostedTransaction> GetUnmatchedPostedTransactions(Guid accountId);
        IEnumerable<PostedTransferTransaction> GetUnmatchedPostedTransferTransactions(Guid accountId);

        // For roll-off logic
        IEnumerable<FutureSingleTransaction> GetRealizedFutureSingleTransactions(DateTime cutoff);
        IEnumerable<FutureTransferTransaction> GetRealizedFutureTransferTransactions(DateTime cutoff);

        // For forecasting engine
        IEnumerable<FutureSingleTransaction> GetAllFutureSingleTransactions();
        IEnumerable<FutureTransferTransaction> GetAllFutureTransferTransactions();
        IEnumerable<RecurringSingleTransactionRule> GetAllRecurringSingleRules();
        IEnumerable<RecurringTransferRule> GetAllRecurringTransferRules();
        IEnumerable<ExpenseBudgetRule> GetAllExpenseBudgetRules();

        // ------------------------------------------------------------
        // Split Transaction Rows
        // ------------------------------------------------------------
        void SaveSplits(Guid parentId, IEnumerable<SplitTransactionRow> splits);
        List<SplitTransactionRow> GetSplits(Guid parentId);
        void DeleteSplits(Guid parentId);
    }
}
