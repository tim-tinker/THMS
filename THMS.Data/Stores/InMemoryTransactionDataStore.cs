using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores
{
    public class InMemoryTransactionDataStore : ITransactionDataStore
    {
        private readonly InMemoryTransactionStore _store = new();

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
        // Categories
        // ------------------------------------------------------------

        public void AddCategory(TransactionCategory category) =>
            _store.AddCategory(category);

        public void UpdateCategory(TransactionCategory category) =>
            _store.UpdateCategory(category);

        public void DeleteCategory(Guid id) =>
            _store.DeleteCategory(id);

        public TransactionCategory? GetCategory(Guid id) =>
            _store.GetCategory(id);

        public IEnumerable<TransactionCategory> GetAllCategories() =>
            _store.GetAllCategories();

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
