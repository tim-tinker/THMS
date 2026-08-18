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
        void DeletePostedTransaction(Guid id);

        PostedTransaction? GetPostedTransaction(Guid id);
        IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId);
        IEnumerable<PostedTransaction> GetPostedTransactions(DateTime start, DateTime end);


        // ------------------------------------------------------------
        // Posted Transfer Transactions (ledger-level)
        // ------------------------------------------------------------
        void AddPostedTransferTransaction(PostedTransferTransaction transaction);
        void UpdatePostedTransferTransaction(PostedTransferTransaction transaction);
        void DeletePostedTransferTransaction(Guid id);

        PostedTransferTransaction? GetPostedTransferTransaction(Guid id);
        IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId);
        IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(DateTime start, DateTime end);


        // ------------------------------------------------------------
        // Future Single Transactions
        // ------------------------------------------------------------
        void AddFutureSingleTransaction(FutureSingleTransaction transaction);
        void UpdateFutureSingleTransaction(FutureSingleTransaction transaction);
        void DeleteFutureSingleTransaction(Guid id);

        FutureSingleTransaction? GetFutureSingleTransaction(Guid id);
        IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(Guid accountId);
        IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(DateTime start, DateTime end);


        // ------------------------------------------------------------
        // Future Transfer Transactions
        // ------------------------------------------------------------
        void AddFutureTransferTransaction(FutureTransferTransaction transaction);
        void UpdateFutureTransferTransaction(FutureTransferTransaction transaction);
        void DeleteFutureTransferTransaction(Guid id);

        FutureTransferTransaction? GetFutureTransferTransaction(Guid id);
        IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(Guid accountId);
        IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(DateTime start, DateTime end);


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
        // Categories
        // ------------------------------------------------------------
        void AddCategory(TransactionCategory category);
        void UpdateCategory(TransactionCategory category);
        void DeleteCategory(Guid id);

        TransactionCategory? GetCategory(Guid id);
        IEnumerable<TransactionCategory> GetAllCategories();


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
    }
}
