using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
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

            foreach (var expected in _transactions.GetAllFutureTransferTransactions()
                         .Where(p => !p.IsRealized
                                     && p.Origin == ExpectedOrigin.StatementPay
                                     && p.OriginId == id)
                         .ToList())
            {
                _transactions.DeleteFutureTransferTransaction(expected.Id);
            }

            _statements.Delete(id);
        }

        public void SaveStatement(AccountStatement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);
            AccountStatementValidator.EnsureValid(statement);
            _statements.Save(statement);
            var account = _accounts.GetAllAccounts().FirstOrDefault(a => a.Id == statement.AccountId);
            if (account is not null)
                SyncAutoPayForAccount(account);
            else
                SyncAutoPayForStatement(statement);
        }

        public void SyncAutoPayForAccount(Account account)
        {
            ArgumentNullException.ThrowIfNull(account);
            DropSupersededStatementIntents(account.Id);
            var latest = PayableStatements.Latest(_statements.GetForAccount(account.Id));
            if (latest is not null)
                SyncAutoPayForStatement(latest, account);
        }

        private void DropSupersededStatementIntents(Guid accountId)
        {
            var keepId = PayableStatements.Latest(_statements.GetForAccount(accountId))?.Id;
            var statementIds = _statements.GetForAccount(accountId).Select(s => s.Id).ToHashSet();
            foreach (var expected in _transactions.GetAllFutureTransferTransactions()
                         .Where(p => !p.IsRealized
                                     && p.Origin == ExpectedOrigin.StatementPay
                                     && p.OriginId is Guid sourceId
                                     && statementIds.Contains(sourceId)
                                     && sourceId != keepId)
                         .ToList())
            {
                _transactions.DeleteFutureTransferTransaction(expected.Id);
            }
        }

        private void SyncAutoPayForStatement(AccountStatement statement, Account? account = null)
        {
            account ??= _accounts.GetAllAccounts().FirstOrDefault(a => a.Id == statement.AccountId);
            var existing = _transactions.GetAllFutureTransferTransactions()
                .FirstOrDefault(p => !p.IsRealized
                                     && p.Origin == ExpectedOrigin.StatementPay
                                     && p.OriginId == statement.Id);
            var latest = PayableStatements.Latest(_statements.GetForAccount(statement.AccountId));

            if (statement is BankStatement || statement.AmountDue <= 0 || latest?.Id != statement.Id)
            {
                if (existing is not null)
                    _transactions.DeleteFutureTransferTransaction(existing.Id);
                return;
            }

            if (account is null || !account.AutoPay ||
                account.AutoPayFromAccountId is not Guid funding || funding == Guid.Empty)
            {
                return;
            }

            if (existing is not null)
            {
                existing.ToAccountId = statement.AccountId;
                existing.FromAccountId = funding;
                existing.Amount = Math.Abs(statement.AmountDue);
                existing.Date = statement.DueDate.Date;
                existing.Status = ExpectedStatus.Scheduled;
                if (string.IsNullOrWhiteSpace(existing.Description))
                    existing.Description = string.IsNullOrWhiteSpace(statement.Notes) ? account.Name : statement.Notes.Trim();
                _transactions.UpdateFutureTransferTransaction(existing);
                return;
            }

            _transactions.AddFutureTransferTransaction(new FutureTransferTransaction
            {
                FromAccountId = funding,
                ToAccountId = statement.AccountId,
                Amount = Math.Abs(statement.AmountDue),
                Date = statement.DueDate.Date,
                Status = ExpectedStatus.Scheduled,
                Origin = ExpectedOrigin.StatementPay,
                OriginId = statement.Id,
                StatementId = statement.Id,
                IsUserCreated = true,
                IsPlannedPayment = true,
                Description = string.IsNullOrWhiteSpace(statement.Notes) ? account.Name : statement.Notes.Trim()
            });
        }
    }
}
