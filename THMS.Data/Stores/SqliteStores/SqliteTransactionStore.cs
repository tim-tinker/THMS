using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Planning;
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
        private readonly TransactionReconciliationsTable _reconciliations = new();

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
            _reconciliations.InitializeSchema(conn);
            _categories.MigrateLegacyCategoryStrings(conn);
            MigratePaymentIntentsToExpected(conn);
        }

        private void MigratePaymentIntentsToExpected(SqliteConnection conn)
        {
            foreach (var intent in _paymentIntents.GetAll(conn).ToList())
            {
                if (intent.Status != PaymentIntentStatus.Scheduled)
                    continue;
                if (_futureTransfers.GetById(conn, intent.Id) is not null)
                    continue;

                var origin = intent.Source switch
                {
                    PaymentIntentSource.Statement => ExpectedOrigin.StatementPay,
                    PaymentIntentSource.RecurringSingle => ExpectedOrigin.RecurringSingle,
                    PaymentIntentSource.RecurringTransfer => ExpectedOrigin.RecurringTransfer,
                    _ => ExpectedOrigin.Pay
                };
                _futureTransfers.Add(conn, new FutureTransferTransaction
                {
                    Id = intent.Id,
                    FromAccountId = intent.FundingAccountId,
                    ToAccountId = intent.DestinationAccountId,
                    Date = intent.PayDate.Date,
                    Amount = Math.Abs(intent.Amount),
                    Description = intent.Description,
                    Origin = origin,
                    OriginId = intent.SourceId,
                    Status = ExpectedStatus.Scheduled,
                    IsUserCreated = true,
                    IsPlannedPayment = true,
                    StatementId = intent.Source == PaymentIntentSource.Statement ? intent.SourceId : null
                });
                _paymentIntents.Delete(conn, intent.Id);
            }
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
        public TransactionReconciliationsTable Reconciliations => _reconciliations;
    }
}
