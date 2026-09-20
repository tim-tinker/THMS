using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Planning;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators.Finance
{
    public class PlanningOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly IAccountStatementDataStore _statements;

        public PlanningOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetAccountStatementStore())
        {
        }

        public PlanningOrchestrator(
            IAccountDataStore accounts,
            ITransactionDataStore transactions,
            IAccountStatementDataStore statements)
        {
            _accounts = accounts;
            _transactions = transactions;
            _statements = statements;
        }

        public IAccountStatementDataStore GetStatementStore() => _statements;

        public IReadOnlyList<Account> GetAccounts() => _accounts.GetAllAccounts().ToList();

        public AccountStatement? GetStatement(Guid id) => _statements.Get(id);

        public List<AccountStatement> GetAllStatements() =>
            _accounts.GetAllAccounts()
                .SelectMany(account => _statements.GetForAccount(account.Id))
                .OrderByDescending(s => s.StatementDate)
                .ThenBy(s => s.Type.ToString())
                .ToList();

        public List<AccountStatementListRow> GetStatementListRows(Guid accountId)
        {
            return _statements.GetForAccount(accountId)
                .OrderByDescending(s => s.StatementDate)
                .ThenByDescending(s => s.DueDate)
                .Select(AccountStatementListRow.From)
                .ToList();
        }

        public void DeleteStatement(Guid id)
        {
            if (_statements.Get(id) is null)
                throw new InvalidOperationException("Statement was not found.");

            foreach (var intent in _transactions.GetScheduledPaymentIntents()
                         .Where(p => p.Source == PaymentIntentSource.Statement && p.SourceId == id)
                         .ToList())
            {
                _transactions.DeletePaymentIntent(intent.Id);
            }

            _statements.Delete(id);
        }

        public void SaveStatement(AccountStatement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);
            AccountStatementValidator.EnsureValid(statement);
            _statements.Save(statement);
        }
    }
}
