using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteTransactionStore
    {
        private readonly PostedTransactionsTable _posted = new();
        private readonly PostedTransferTransactionsTable _postedTransfers = new();
        private readonly FutureSingleTransactionsTable _futureSingles = new();
        private readonly FutureTransferTransactionsTable _futureTransfers = new();
        private readonly RecurringSingleTransactionRulesTable _recurringSingles = new();
        private readonly RecurringTransferRulesTable _recurringTransfers = new();
        private readonly ExpenseBudgetRulesTable _expenseBudgetRules = new();
        private readonly ExpenseBudgetHistoryTable _expenseBudgetHistory = new();
        private readonly ExpenseCategoriesTable _categories = new();
        private readonly CategoryAssignmentHistoryTable _assignments = new();
        private readonly SplitTransactionRowsTable _splits = new();
        private readonly PaymentIntentsTable _paymentIntents = new();

        public void InitializeSchema(SqliteConnection conn)
        {
            _posted.InitializeSchema(conn);
            _postedTransfers.InitializeSchema(conn);
            _futureSingles.InitializeSchema(conn);
            _futureTransfers.InitializeSchema(conn);
            _recurringSingles.InitializeSchema(conn);
            _recurringTransfers.InitializeSchema(conn);
            _expenseBudgetRules.InitializeSchema(conn);
            _expenseBudgetHistory.InitializeSchema(conn);
            _categories.InitializeSchema(conn);
            _assignments.InitializeSchema(conn);
            _splits.InitializeSchema(conn);
            _paymentIntents.InitializeSchema(conn);
            _categories.MigrateLegacyCategoryStrings(conn);
            DropUnrealizedPlannedPayments(conn);
        }

        private static void DropUnrealizedPlannedPayments(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM FutureSingleTransactions WHERE IsPlannedPayment = 1 AND IsRealized = 0;
                DELETE FROM FutureTransferTransactions WHERE IsPlannedPayment = 1 AND IsRealized = 0;";
            cmd.ExecuteNonQuery();
        }

        public PostedTransactionsTable Posted => _posted;
        public PostedTransferTransactionsTable PostedTransfers => _postedTransfers;
        public FutureSingleTransactionsTable FutureSingles => _futureSingles;
        public FutureTransferTransactionsTable FutureTransfers => _futureTransfers;
        public RecurringSingleTransactionRulesTable RecurringSingles => _recurringSingles;
        public RecurringTransferRulesTable RecurringTransfers => _recurringTransfers;
        public ExpenseBudgetRulesTable ExpenseBudgetRules => _expenseBudgetRules;
        public ExpenseBudgetHistoryTable ExpenseBudgetHistory => _expenseBudgetHistory;
        public ExpenseCategoriesTable Categories => _categories;
        public CategoryAssignmentHistoryTable Assignments => _assignments;
        public SplitTransactionRowsTable Splits => _splits;
        public PaymentIntentsTable PaymentIntents => _paymentIntents;
    }
}
