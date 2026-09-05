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
        private readonly TransactionCategoriesTable _categories = new();

        public void InitializeSchema(SqliteConnection conn)
        {
            _posted.InitializeSchema(conn);
            _postedTransfers.InitializeSchema(conn);
            _futureSingles.InitializeSchema(conn);
            _futureTransfers.InitializeSchema(conn);
            _recurringSingles.InitializeSchema(conn);
            _recurringTransfers.InitializeSchema(conn);
            _expenseBudgetRules.InitializeSchema(conn);
            _categories.InitializeSchema(conn);
        }

        public PostedTransactionsTable Posted => _posted;
        public PostedTransferTransactionsTable PostedTransfers => _postedTransfers;
        public FutureSingleTransactionsTable FutureSingles => _futureSingles;
        public FutureTransferTransactionsTable FutureTransfers => _futureTransfers;
        public RecurringSingleTransactionRulesTable RecurringSingles => _recurringSingles;
        public RecurringTransferRulesTable RecurringTransfers => _recurringTransfers;
        public ExpenseBudgetRulesTable ExpenseBudgetRules => _expenseBudgetRules;
        public TransactionCategoriesTable Categories => _categories;
    }
}
