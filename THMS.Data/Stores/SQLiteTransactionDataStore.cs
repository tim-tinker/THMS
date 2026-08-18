using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqliteStores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteTransactionDataStore : ITransactionDataStore
    {
        private readonly string _connectionString;
        private readonly SqliteTransactionStore _store = new();

        public SQLiteTransactionDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = OpenConnection();
            _store.InitializeSchema(conn);
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // ------------------------------------------------------------
        // Posted Transactions
        // ------------------------------------------------------------

        public void AddPostedTransaction(PostedTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.Posted.Add(conn, transaction);
        }

        public void UpdatePostedTransaction(PostedTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.Posted.Update(conn, transaction);
        }

        public void DeletePostedTransaction(Guid id)
        {
            using var conn = OpenConnection();
            _store.Posted.Delete(conn, id);
        }

        public PostedTransaction? GetPostedTransaction(Guid id)
        {
            using var conn = OpenConnection();
            return _store.Posted.GetById(conn, id);
        }

        public IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.Posted.GetByAccount(conn, accountId).ToList();
        }

        public IEnumerable<PostedTransaction> GetPostedTransactions(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _store.Posted.GetByDateRange(conn, start, end).ToList();
        }

        // ------------------------------------------------------------
        // Posted Transfer Transactions
        // ------------------------------------------------------------

        public void AddPostedTransferTransaction(PostedTransferTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.PostedTransfers.Add(conn, transaction);
        }

        public void UpdatePostedTransferTransaction(PostedTransferTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.PostedTransfers.Update(conn, transaction);
        }

        public void DeletePostedTransferTransaction(Guid id)
        {
            using var conn = OpenConnection();
            _store.PostedTransfers.Delete(conn, id);
        }

        public PostedTransferTransaction? GetPostedTransferTransaction(Guid id)
        {
            using var conn = OpenConnection();
            return _store.PostedTransfers.GetById(conn, id);
        }

        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.PostedTransfers.GetByAccount(conn, accountId).ToList();
        }

        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _store.PostedTransfers.GetByDateRange(conn, start, end).ToList();
        }

        // ------------------------------------------------------------
        // Future Single Transactions
        // ------------------------------------------------------------

        public void AddFutureSingleTransaction(FutureSingleTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.FutureSingles.Add(conn, transaction);
        }

        public void UpdateFutureSingleTransaction(FutureSingleTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.FutureSingles.Update(conn, transaction);
        }

        public void DeleteFutureSingleTransaction(Guid id)
        {
            using var conn = OpenConnection();
            _store.FutureSingles.Delete(conn, id);
        }

        public FutureSingleTransaction? GetFutureSingleTransaction(Guid id)
        {
            using var conn = OpenConnection();
            return _store.FutureSingles.GetById(conn, id);
        }

        public IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.FutureSingles.GetByAccount(conn, accountId).ToList();
        }

        public IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _store.FutureSingles.GetByDateRange(conn, start, end).ToList();
        }

        // ------------------------------------------------------------
        // Future Transfer Transactions
        // ------------------------------------------------------------

        public void AddFutureTransferTransaction(FutureTransferTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.FutureTransfers.Add(conn, transaction);
        }

        public void UpdateFutureTransferTransaction(FutureTransferTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.FutureTransfers.Update(conn, transaction);
        }

        public void DeleteFutureTransferTransaction(Guid id)
        {
            using var conn = OpenConnection();
            _store.FutureTransfers.Delete(conn, id);
        }

        public FutureTransferTransaction? GetFutureTransferTransaction(Guid id)
        {
            using var conn = OpenConnection();
            return _store.FutureTransfers.GetById(conn, id);
        }

        public IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.FutureTransfers.GetByAccount(conn, accountId).ToList();
        }

        public IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _store.FutureTransfers.GetByDateRange(conn, start, end).ToList();
        }

        // ------------------------------------------------------------
        // Recurring Single Rules
        // ------------------------------------------------------------

        public void AddRecurringSingleRule(RecurringSingleTransactionRule rule)
        {
            using var conn = OpenConnection();
            _store.RecurringSingles.Add(conn, rule);
        }

        public void UpdateRecurringSingleRule(RecurringSingleTransactionRule rule)
        {
            using var conn = OpenConnection();
            _store.RecurringSingles.Update(conn, rule);
        }

        public void DeleteRecurringSingleRule(Guid id)
        {
            using var conn = OpenConnection();
            _store.RecurringSingles.Delete(conn, id);
        }

        public RecurringSingleTransactionRule? GetRecurringSingleRule(Guid id)
        {
            using var conn = OpenConnection();
            return _store.RecurringSingles.GetById(conn, id);
        }

        public IEnumerable<RecurringSingleTransactionRule> GetRecurringSingleRules(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.RecurringSingles.GetByAccount(conn, accountId).ToList();
        }

        // ------------------------------------------------------------
        // Recurring Transfer Rules
        // ------------------------------------------------------------

        public void AddRecurringTransferRule(RecurringTransferRule rule)
        {
            using var conn = OpenConnection();
            _store.RecurringTransfers.Add(conn, rule);
        }

        public void UpdateRecurringTransferRule(RecurringTransferRule rule)
        {
            using var conn = OpenConnection();
            _store.RecurringTransfers.Update(conn, rule);
        }

        public void DeleteRecurringTransferRule(Guid id)
        {
            using var conn = OpenConnection();
            _store.RecurringTransfers.Delete(conn, id);
        }

        public RecurringTransferRule? GetRecurringTransferRule(Guid id)
        {
            using var conn = OpenConnection();
            return _store.RecurringTransfers.GetById(conn, id);
        }

        public IEnumerable<RecurringTransferRule> GetRecurringTransferRules(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.RecurringTransfers.GetByAccount(conn, accountId).ToList();
        }

        // ------------------------------------------------------------
        // Categories
        // ------------------------------------------------------------

        public void AddCategory(TransactionCategory category)
        {
            using var conn = OpenConnection();
            _store.Categories.Add(conn, category);
        }

        public void UpdateCategory(TransactionCategory category)
        {
            using var conn = OpenConnection();
            _store.Categories.Update(conn, category);
        }

        public void DeleteCategory(Guid id)
        {
            using var conn = OpenConnection();
            _store.Categories.Delete(conn, id);
        }

        public TransactionCategory? GetCategory(Guid id)
        {
            using var conn = OpenConnection();
            return _store.Categories.GetById(conn, id);
        }

        public IEnumerable<TransactionCategory> GetAllCategories()
        {
            using var conn = OpenConnection();
            return _store.Categories.GetAll(conn).ToList();
        }

        // ------------------------------------------------------------
        // Utility Queries
        // ------------------------------------------------------------

        public IEnumerable<PostedTransaction> GetUnmatchedPostedTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.Posted.GetUnmatchedByAccount(conn, accountId).ToList();
        }

        public IEnumerable<PostedTransferTransaction> GetUnmatchedPostedTransferTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.PostedTransfers.GetUnmatchedByAccount(conn, accountId).ToList();
        }

        public IEnumerable<FutureSingleTransaction> GetRealizedFutureSingleTransactions(DateTime cutoff)
        {
            using var conn = OpenConnection();
            return _store.FutureSingles.GetRealized(conn, cutoff).ToList();
        }

        public IEnumerable<FutureTransferTransaction> GetRealizedFutureTransferTransactions(DateTime cutoff)
        {
            using var conn = OpenConnection();
            return _store.FutureTransfers.GetRealized(conn, cutoff).ToList();
        }

        public IEnumerable<FutureSingleTransaction> GetAllFutureSingleTransactions()
        {
            using var conn = OpenConnection();
            return _store.FutureSingles.GetAll(conn).ToList();
        }

        public IEnumerable<FutureTransferTransaction> GetAllFutureTransferTransactions()
        {
            using var conn = OpenConnection();
            return _store.FutureTransfers.GetAll(conn).ToList();
        }

        public IEnumerable<RecurringSingleTransactionRule> GetAllRecurringSingleRules()
        {
            using var conn = OpenConnection();
            return _store.RecurringSingles.GetAll(conn).ToList();
        }

        public IEnumerable<RecurringTransferRule> GetAllRecurringTransferRules()
        {
            using var conn = OpenConnection();
            return _store.RecurringTransfers.GetAll(conn).ToList();
        }
    }
}
