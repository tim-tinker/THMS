using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqliteStores;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteTransactionDataStore : ITransactionDataStore, ICategoryDataStore
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
            PersistIncomingSplits(conn, transaction);
        }

        public void UpdatePostedTransaction(PostedTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.Posted.Update(conn, transaction);
        }

        public void ReplacePostedTransaction(PostedTransaction replacement)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            var existing = _store.Posted.GetById(conn, replacement.Id);
            if (existing is null)
                throw new InvalidOperationException(
                    $"Posted transaction {replacement.Id} not found.");

            _store.Posted.Delete(conn, replacement.Id);

            if (replacement is PostedTransferTransaction transfer)
                _store.PostedTransfers.Add(conn, transfer);
            else
                _store.Posted.Add(conn, replacement);

            tx.Commit();
        }

        public void DeletePostedTransaction(Guid id)
        {
            using var conn = OpenConnection();
            _store.Posted.Delete(conn, id);
            _store.Splits.DeleteByParent(conn, id);
        }

        public PostedTransaction? GetPostedTransaction(Guid id)
        {
            using var conn = OpenConnection();
            return AttachOne(conn, _store.Posted.GetById(conn, id));
        }

        public IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.Posted.GetByAccount(conn, accountId));
        }

        public IEnumerable<PostedTransaction> GetPostedTransactions(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.Posted.GetByDateRange(conn, start, end));
        }

        public IEnumerable<PostedTransaction> GetPostedTransactions(Guid accountId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.Posted.GetByAccountAndDateRange(conn, accountId, start, end));
        }

        public decimal SumPostedAmountsBefore(Guid accountId, DateTime before)
        {
            using var conn = OpenConnection();
            return _store.Posted.SumAmountBefore(conn, accountId, before);
        }

        public decimal SumPostedAmountsAfter(Guid accountId, DateTime after)
        {
            using var conn = OpenConnection();
            return _store.Posted.SumAmountAfter(conn, accountId, after);
        }

        public DateTime? GetLatestPostedTransactionDate(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.Posted.GetLatest(conn, accountId)?.Date;
        }

        // ------------------------------------------------------------
        // Posted Transfer Transactions
        // ------------------------------------------------------------

        public void AddPostedTransferTransaction(PostedTransferTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.PostedTransfers.Add(conn, transaction);
            PersistIncomingSplits(conn, transaction);
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
            _store.Splits.DeleteByParent(conn, id);
        }

        public PostedTransferTransaction? GetPostedTransferTransaction(Guid id)
        {
            using var conn = OpenConnection();
            return AttachOne(conn, _store.PostedTransfers.GetById(conn, id));
        }

        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.PostedTransfers.GetByAccount(conn, accountId));
        }

        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.PostedTransfers.GetByDateRange(conn, start, end));
        }

        public IEnumerable<PostedTransferTransaction> GetPostedTransferTransactions(Guid accountId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.PostedTransfers.GetByAccountAndDateRange(conn, accountId, start, end));
        }

        public decimal SumPostedTransferAmountsBefore(Guid accountId, DateTime before)
        {
            using var conn = OpenConnection();
            return _store.PostedTransfers.SumAmountBefore(conn, accountId, before);
        }

        public decimal SumPostedTransferAmountsAfter(Guid accountId, DateTime after)
        {
            using var conn = OpenConnection();
            return _store.PostedTransfers.SumAmountAfter(conn, accountId, after);
        }

        public DateTime? GetLatestPostedTransferTransactionDate(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.PostedTransfers.GetLatest(conn, accountId)?.Date;
        }

        // ------------------------------------------------------------
        // Future Single Transactions
        // ------------------------------------------------------------

        public void AddFutureSingleTransaction(FutureSingleTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.FutureSingles.Add(conn, transaction);
            PersistIncomingSplits(conn, transaction);
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
            _store.Splits.DeleteByParent(conn, id);
        }

        public FutureSingleTransaction? GetFutureSingleTransaction(Guid id)
        {
            using var conn = OpenConnection();
            return AttachOne(conn, _store.FutureSingles.GetById(conn, id));
        }

        public IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureSingles.GetByAccount(conn, accountId));
        }

        public IEnumerable<FutureSingleTransaction> GetFutureSingleTransactions(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureSingles.GetByDateRange(conn, start, end));
        }

        public List<FutureSingleTransaction> GetPlannedPayments(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureSingles.GetPlanned(conn, accountId));
        }

        public List<FutureSingleTransaction> GetAllPlannedPayments()
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureSingles.GetAllPlanned(conn));
        }

        // ------------------------------------------------------------
        // Future Transfer Transactions
        // ------------------------------------------------------------

        public void AddFutureTransferTransaction(FutureTransferTransaction transaction)
        {
            using var conn = OpenConnection();
            _store.FutureTransfers.Add(conn, transaction);
            PersistIncomingSplits(conn, transaction);
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
            _store.Splits.DeleteByParent(conn, id);
        }

        public FutureTransferTransaction? GetFutureTransferTransaction(Guid id)
        {
            using var conn = OpenConnection();
            return AttachOne(conn, _store.FutureTransfers.GetById(conn, id));
        }

        public IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureTransfers.GetByAccount(conn, accountId));
        }

        public IEnumerable<FutureTransferTransaction> GetFutureTransferTransactions(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureTransfers.GetByDateRange(conn, start, end));
        }

        public List<FutureTransferTransaction> GetPlannedTransfers(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureTransfers.GetPlanned(conn, accountId));
        }

        public List<FutureTransferTransaction> GetAllPlannedTransfers()
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureTransfers.GetAllPlanned(conn));
        }

        // ------------------------------------------------------------
        // Recurring Single Rules
        // ------------------------------------------------------------

        public void AddRecurringSingleRule(RecurringSingleTransactionRule rule)
        {
            using var conn = OpenConnection();
            var existing = _store.RecurringSingles.GetByAccount(conn, rule.AccountId)
                .FirstOrDefault(r => RecurringRulePattern.Matches(r, rule));
            if (existing is not null)
            {
                if (existing.IsUserCreated && !rule.IsUserCreated)
                    return;

                rule.Id = existing.Id;
                rule.IsUserCreated = existing.IsUserCreated || rule.IsUserCreated;
                _store.RecurringSingles.Update(conn, rule);
                PersistIncomingSplits(conn, rule);
                return;
            }

            _store.RecurringSingles.Add(conn, rule);
            PersistIncomingSplits(conn, rule);
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
            _store.Splits.DeleteByParent(conn, id);
        }

        public RecurringSingleTransactionRule? GetRecurringSingleRule(Guid id)
        {
            using var conn = OpenConnection();
            return AttachOne(conn, _store.RecurringSingles.GetById(conn, id));
        }

        public IEnumerable<RecurringSingleTransactionRule> GetRecurringSingleRules(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.RecurringSingles.GetByAccount(conn, accountId));
        }

        // ------------------------------------------------------------
        // Recurring Transfer Rules
        // ------------------------------------------------------------

        public void AddRecurringTransferRule(RecurringTransferRule rule)
        {
            using var conn = OpenConnection();
            var existing = _store.RecurringTransfers.GetByAccount(conn, rule.FromAccountId)
                .FirstOrDefault(r => RecurringRulePattern.Matches(r, rule));
            if (existing is not null)
            {
                if (existing.IsUserCreated && !rule.IsUserCreated)
                    return;

                rule.Id = existing.Id;
                rule.IsUserCreated = existing.IsUserCreated || rule.IsUserCreated;
                _store.RecurringTransfers.Update(conn, rule);
                PersistIncomingSplits(conn, rule);
                return;
            }

            _store.RecurringTransfers.Add(conn, rule);
            PersistIncomingSplits(conn, rule);
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
            _store.Splits.DeleteByParent(conn, id);
        }

        public RecurringTransferRule? GetRecurringTransferRule(Guid id)
        {
            using var conn = OpenConnection();
            return AttachOne(conn, _store.RecurringTransfers.GetById(conn, id));
        }

        public IEnumerable<RecurringTransferRule> GetRecurringTransferRules(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.RecurringTransfers.GetByAccount(conn, accountId));
        }

        // ------------------------------------------------------------
        // Expense Budget Rules
        // ------------------------------------------------------------

        public void AddExpenseBudgetRule(ExpenseBudgetRule rule)
        {
            using var conn = OpenConnection();
            _store.ExpenseBudgetRules.Add(conn, rule);
        }

        public void UpdateExpenseBudgetRule(ExpenseBudgetRule rule)
        {
            using var conn = OpenConnection();
            _store.ExpenseBudgetRules.Update(conn, rule);
        }

        public void DeleteExpenseBudgetRule(Guid ruleId)
        {
            using var conn = OpenConnection();
            _store.ExpenseBudgetHistory.DeleteByRule(conn, ruleId);
            _store.ExpenseBudgetRules.Delete(conn, ruleId);
        }

        public ExpenseBudgetRule? GetExpenseBudgetRule(Guid ruleId)
        {
            using var conn = OpenConnection();
            return _store.ExpenseBudgetRules.GetById(conn, ruleId);
        }

        public IEnumerable<ExpenseBudgetRule> GetExpenseBudgetRules(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.ExpenseBudgetRules.GetByAccount(conn, accountId).ToList();
        }

        public IEnumerable<ExpenseBudgetRule> GetAllExpenseBudgetRules()
        {
            using var conn = OpenConnection();
            return _store.ExpenseBudgetRules.GetAll(conn).ToList();
        }

        public void AddExpenseBudgetHistory(ExpenseBudgetHistory history)
        {
            using var conn = OpenConnection();
            _store.ExpenseBudgetHistory.Add(conn, history);
        }

        public void UpdateExpenseBudgetHistory(ExpenseBudgetHistory history)
        {
            using var conn = OpenConnection();
            _store.ExpenseBudgetHistory.Update(conn, history);
        }

        public ExpenseBudgetHistory? GetExpenseBudgetHistoryById(Guid historyId)
        {
            using var conn = OpenConnection();
            return _store.ExpenseBudgetHistory.GetById(conn, historyId);
        }

        public IEnumerable<ExpenseBudgetHistory> GetExpenseBudgetHistory(Guid budgetRuleId)
        {
            using var conn = OpenConnection();
            return _store.ExpenseBudgetHistory.GetByRule(conn, budgetRuleId).ToList();
        }

        public ExpenseBudgetHistory? GetActiveBudgetHistory(Guid budgetRuleId)
        {
            using var conn = OpenConnection();
            return _store.ExpenseBudgetHistory.GetActive(conn, budgetRuleId);
        }

        public IEnumerable<ExpenseBudgetHistory> GetBudgetHistoryForPeriod(
            Guid budgetRuleId,
            DateTime periodStart,
            DateTime periodEnd)
        {
            using var conn = OpenConnection();
            return _store.ExpenseBudgetHistory.GetForPeriod(conn, budgetRuleId, periodStart, periodEnd).ToList();
        }

        public void CloseBudgetHistory(Guid historyId)
        {
            using var conn = OpenConnection();
            var history = _store.ExpenseBudgetHistory.GetById(conn, historyId);
            if (history is null)
                return;

            history.IsClosed = true;
            history.RecalculateRemaining();
            _store.ExpenseBudgetHistory.Update(conn, history);
        }

        // ------------------------------------------------------------
        // Categories
        // ------------------------------------------------------------

        public void AddCategory(ExpenseCategory category)
        {
            using var conn = OpenConnection();
            _store.Categories.Add(conn, category);
        }

        public void UpdateCategory(ExpenseCategory category)
        {
            using var conn = OpenConnection();
            _store.Categories.Update(conn, category);
        }

        public void DeleteCategory(Guid categoryId) =>
            DeactivateCategory(categoryId);

        public void DeactivateCategory(Guid categoryId)
        {
            using var conn = OpenConnection();
            _store.Categories.SoftDelete(conn, categoryId);
        }

        public void MergeCategories(Guid keepCategoryId, Guid retireCategoryId)
        {
            if (keepCategoryId == retireCategoryId)
                throw new InvalidOperationException("A category cannot be merged into itself.");

            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            var keep = _store.Categories.GetById(conn, keepCategoryId)
                ?? throw new InvalidOperationException($"Category {keepCategoryId} was not found.");
            var retire = _store.Categories.GetById(conn, retireCategoryId)
                ?? throw new InvalidOperationException($"Category {retireCategoryId} was not found.");

            if (keep.ParentCategoryId == retire.Id)
            {
                keep.ParentCategoryId = retire.ParentCategoryId;
                _store.Categories.Update(conn, keep);
            }

            foreach (var child in _store.Categories.GetAll(conn, includeInactive: true)
                         .Where(c => c.ParentCategoryId == retire.Id && c.Id != keep.Id))
            {
                child.ParentCategoryId = keep.Id;
                _store.Categories.Update(conn, child);
            }

            SqliteCategoryColumns.Retarget(conn, "PostedTransactions", retire.Id, keep.Id, keep.Name);
            SqliteCategoryColumns.Retarget(conn, "PostedTransferTransactions", retire.Id, keep.Id, keep.Name);
            SqliteCategoryColumns.Retarget(conn, "FutureSingleTransactions", retire.Id, keep.Id, keep.Name);
            SqliteCategoryColumns.Retarget(conn, "FutureTransferTransactions", retire.Id, keep.Id, keep.Name);
            SqliteCategoryColumns.Retarget(conn, "RecurringSingleTransactionRules", retire.Id, keep.Id, keep.Name);
            SqliteCategoryColumns.Retarget(conn, "RecurringTransferRules", retire.Id, keep.Id, keep.Name);
            SqliteCategoryColumns.Retarget(conn, "SplitTransactionRows", retire.Id, keep.Id, keep.Name);

            foreach (var rule in _store.ExpenseBudgetRules.GetAll(conn))
            {
                if (!rule.IncludedCategoryIds.Contains(retire.Id))
                    continue;

                rule.IncludedCategoryIds = rule.IncludedCategoryIds
                    .Select(id => id == retire.Id ? keep.Id : id)
                    .Distinct()
                    .ToList();
                _store.ExpenseBudgetRules.Update(conn, rule);
            }

            _store.Assignments.Retarget(conn, retire.Id, keep.Id);
            _store.Categories.SoftDelete(conn, retire.Id);
            tx.Commit();
        }

        public ExpenseCategory? GetCategory(Guid categoryId)
        {
            using var conn = OpenConnection();
            return _store.Categories.GetById(conn, categoryId);
        }

        public IEnumerable<ExpenseCategory> GetAllCategories(bool includeInactive = false)
        {
            using var conn = OpenConnection();
            return _store.Categories.GetAll(conn, includeInactive).ToList();
        }

        public IEnumerable<ExpenseCategory> GetChildCategories(Guid parentCategoryId)
        {
            using var conn = OpenConnection();
            return _store.Categories.GetChildren(conn, parentCategoryId).ToList();
        }

        public IEnumerable<ExpenseCategory> GetCategoryTree(bool includeInactive = true)
        {
            using var conn = OpenConnection();
            return _store.Categories.GetAll(conn, includeInactive).ToList();
        }

        public CategoryUsage CountCategoryUsage(Guid categoryId)
        {
            using var conn = OpenConnection();
            return new CategoryUsage
            {
                TransactionCount =
                    SqliteCategoryColumns.CountUnsplit(conn, "PostedTransactions", categoryId) +
                    SqliteCategoryColumns.CountUnsplit(conn, "PostedTransferTransactions", categoryId) +
                    SqliteCategoryColumns.CountUnsplit(conn, "FutureSingleTransactions", categoryId) +
                    SqliteCategoryColumns.CountUnsplit(conn, "FutureTransferTransactions", categoryId) +
                    SqliteCategoryColumns.Count(conn, "SplitTransactionRows", categoryId),
                RecurringRuleCount =
                    SqliteCategoryColumns.CountUnsplit(conn, "RecurringSingleTransactionRules", categoryId) +
                    SqliteCategoryColumns.CountUnsplit(conn, "RecurringTransferRules", categoryId),
                BudgetRuleCount = _store.ExpenseBudgetRules.GetAll(conn)
                    .Count(r => r.IncludedCategoryIds.Contains(categoryId)),
                LearnedMappingCount = _store.Assignments.CountByCategory(conn, categoryId)
            };
        }

        public void EnsureDefaultCategories()
        {
            using var conn = OpenConnection();
            _store.Categories.EnsureDefaults(conn);
        }

        public void UpsertAssignment(string normalizedDescription, Guid categoryId)
        {
            using var conn = OpenConnection();
            _store.Assignments.Upsert(conn, normalizedDescription, categoryId);
        }

        public CategoryAssignment? GetAssignment(string normalizedDescription)
        {
            using var conn = OpenConnection();
            return _store.Assignments.Get(conn, normalizedDescription);
        }

        // ------------------------------------------------------------
        // Utility Queries
        // ------------------------------------------------------------

        public IEnumerable<PostedTransaction> GetUnmatchedPostedTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.Posted.GetUnmatchedByAccount(conn, accountId));
        }

        public IEnumerable<PostedTransferTransaction> GetUnmatchedPostedTransferTransactions(Guid accountId)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.PostedTransfers.GetUnmatchedByAccount(conn, accountId));
        }

        public IEnumerable<FutureSingleTransaction> GetRealizedFutureSingleTransactions(DateTime cutoff)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureSingles.GetRealized(conn, cutoff));
        }

        public IEnumerable<FutureTransferTransaction> GetRealizedFutureTransferTransactions(DateTime cutoff)
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureTransfers.GetRealized(conn, cutoff));
        }

        public IEnumerable<FutureSingleTransaction> GetAllFutureSingleTransactions()
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureSingles.GetAll(conn));
        }

        public IEnumerable<FutureTransferTransaction> GetAllFutureTransferTransactions()
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.FutureTransfers.GetAll(conn));
        }

        public IEnumerable<RecurringSingleTransactionRule> GetAllRecurringSingleRules()
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.RecurringSingles.GetAll(conn));
        }

        public IEnumerable<RecurringTransferRule> GetAllRecurringTransferRules()
        {
            using var conn = OpenConnection();
            return AttachMany(conn, _store.RecurringTransfers.GetAll(conn));
        }

        public void SaveSplits(Guid parentId, IEnumerable<SplitTransactionRow> splits)
        {
            using var conn = OpenConnection();
            _store.Splits.ReplaceAll(conn, parentId, splits);
        }

        public List<SplitTransactionRow> GetSplits(Guid parentId)
        {
            using var conn = OpenConnection();
            return _store.Splits.GetByParent(conn, parentId);
        }

        public void DeleteSplits(Guid parentId)
        {
            using var conn = OpenConnection();
            _store.Splits.DeleteByParent(conn, parentId);
        }

        private void PersistIncomingSplits(SqliteConnection conn, BaseTransaction item)
        {
            if (item.Splits.Count > 0)
                _store.Splits.ReplaceAll(conn, item.Id, item.Splits);
        }

        private T? AttachOne<T>(SqliteConnection conn, T? item) where T : BaseTransaction
        {
            if (item is not null)
                item.Splits = _store.Splits.GetByParent(conn, item.Id);
            return item;
        }

        private List<T> AttachMany<T>(SqliteConnection conn, IEnumerable<T> items) where T : BaseTransaction
        {
            var list = items.ToList();
            if (list.Count == 0)
                return list;

            var lookup = _store.Splits.GetByParents(conn, list.Select(i => i.Id).ToList());
            foreach (var item in list)
                item.Splits = lookup.GetValueOrDefault(item.Id, []);
            return list;
        }
    }
}
