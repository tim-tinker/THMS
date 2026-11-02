using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Logic.Finance.Model;

namespace THMS.Logic.Orchestrators.Finance
{
    public class AccountOrchestrator
    {
        private readonly IAccountDataStore _accountStore;
        private readonly ITransactionDataStore? _transactions;
        private readonly IAccountStatementDataStore? _statements;
        private readonly IInvestmentBookStore? _book;

        public AccountOrchestrator()
        {
            var factory = new DataStoreFactory();
            _accountStore = factory.GetAccountStore();
            _transactions = factory.GetTransactionStore();
            _statements = factory.GetAccountStatementStore();
            _book = factory.GetInvestmentBookStore();
        }

        public AccountOrchestrator(IAccountDataStore accountStore)
            : this(accountStore, null, null, null)
        {
        }

        public AccountOrchestrator(
            IAccountDataStore accountStore,
            ITransactionDataStore? transactions,
            IAccountStatementDataStore? statements,
            IInvestmentBookStore? book)
        {
            _accountStore = accountStore;
            _transactions = transactions;
            _statements = statements;
            _book = book;
        }

        // ------------------------------------------------------------
        // Create or update an account
        // ------------------------------------------------------------
        public void Save(Account account, bool discardActivity = false)
        {
            Validate(account);
            Normalize(account);

            var current = _accountStore.GetAllAccounts().FirstOrDefault(item => item.Id == account.Id);
            if (current != null
                && !AccountConversion.SameKind(current, account)
                && !AccountConversion.CanCarryTransactions(current, account))
            {
                if (!discardActivity)
                    throw new InvalidOperationException(AccountConversion.LossWarning);

                DiscardActivity(account.Id);
            }

            _accountStore.UpsertAccount(account);
        }

        private void DiscardActivity(Guid accountId)
        {
            if (_transactions == null || _statements == null || _book == null)
                throw new InvalidOperationException("Account activity is not available for this conversion.");

            var transferIds = new HashSet<Guid>();
            foreach (var transfer in _transactions.GetPostedTransferTransactions(accountId).ToList())
            {
                transferIds.Add(transfer.Id);
                if (transfer.RelatedPostedTransactionId != Guid.Empty)
                    transferIds.Add(transfer.RelatedPostedTransactionId);
            }

            foreach (var id in transferIds)
                _transactions.DeletePostedTransferTransaction(id);

            foreach (var posted in _transactions.GetPostedTransactions(accountId).ToList())
                _transactions.DeletePostedTransaction(posted.Id);

            foreach (var future in _transactions.GetFutureSingleTransactions(accountId).ToList())
                _transactions.DeleteFutureSingleTransaction(future.Id);

            foreach (var future in _transactions.GetFutureTransferTransactions(accountId).ToList())
                _transactions.DeleteFutureTransferTransaction(future.Id);

            foreach (var rule in _transactions.GetRecurringSingleRules(accountId).ToList())
                _transactions.DeleteRecurringSingleRule(rule.Id);

            foreach (var rule in _transactions.GetRecurringTransferRules(accountId).ToList())
                _transactions.DeleteRecurringTransferRule(rule.Id);

            foreach (var statement in _statements.GetForAccount(accountId).ToList())
                _statements.Delete(statement.Id);

            _book.DeleteAccount(accountId);
        }

        public void Delete(Guid accountId)
        {
            _accountStore.DeleteAccount(accountId);
        }

        // ------------------------------------------------------------
        // Update posted balance + BalanceAsOf after transaction import
        // ------------------------------------------------------------
        public void UpdatePostedBalance(string accountName, decimal newPostedBalance, DateTime asOfDate)
        {
            var account = _accountStore.GetAccount(accountName);
            if (account == null)
                throw new InvalidOperationException($"Account {accountName} not found.");

            switch (account)
            {
                case BankAccount bank:
                    bank.PostedBalance = newPostedBalance;
                    break;

                case CreditAccount credit:
                    credit.PostedBalance = newPostedBalance;
                    break;

                case InvestmentAccount invest when !invest.TracksPositions:
                    invest.CashBalance = newPostedBalance;
                    break;

                case InvestmentAccount:
                    break;

                case LoanAccount loan:
                    loan.Principal = newPostedBalance;
                    break;

                case MortgageAccount mortgage:
                    mortgage.Principal = newPostedBalance;
                    break;

                case InternalAccount internalAcc:
                    // Internal accounts compute balance from transactions,
                    // so no direct posted balance update.
                    break;

                case UntrackedAccount:
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported account type: {account.GetType().Name}");
            }

            account.BalanceAsOf = asOfDate;
            _accountStore.UpsertAccount(account);
        }

        // ------------------------------------------------------------
        // Retrieve helpers
        // ------------------------------------------------------------
        public Account? GetAccount(string name) =>
            _accountStore.GetAccount(name);

        public IEnumerable<Account> GetAllAccounts() =>
            _accountStore.GetAllAccounts();

        public Account? GetAccount(Guid id) =>
            _accountStore.GetAllAccounts().FirstOrDefault(a => a.Id == id);

        public void AdjustStartingBalanceForPostedDelta(Guid accountId, decimal postedBalanceDelta)
        {
            if (postedBalanceDelta == 0)
                return;

            var account = GetAccount(accountId);
            if (account == null)
                throw new InvalidOperationException($"Account {accountId} not found.");

            PostedBalanceCalculator.AdjustStartingBalance(account, postedBalanceDelta);
            _accountStore.UpsertAccount(account);
        }

        // ------------------------------------------------------------
        // Validation
        // ------------------------------------------------------------
        private void Validate(Account account)
        {
            if (string.IsNullOrWhiteSpace(account.Name))
                throw new ArgumentException("Account name is required.");

            if (account.Type == AccountType.CreditCard ||
                account.Type == AccountType.LineOfCredit)
            {
                if (account is CreditAccount credit)
                {
                    if (credit.CreditLimit <= 0)
                        throw new ArgumentException("Credit limit must be positive.");
                }
            }

            if (!account.SupportsAutoPay)
            {
                account.AutoPay = false;
                account.AutoPayFromAccountId = null;
                return;
            }

            if (account.AutoPayFromAccountId is Guid funding && funding != Guid.Empty && funding == account.Id)
                throw new ArgumentException("Pay from a different account.");

            if (!account.AutoPay)
                return;

            if (account.AutoPayFromAccountId is not Guid || account.AutoPayFromAccountId == Guid.Empty)
                throw new ArgumentException("Auto-pay requires a funding account.");
        }

        // ------------------------------------------------------------
        // Normalization
        // ------------------------------------------------------------
        private void Normalize(Account account)
        {
            account.Institution ??= "";
            account.AccountNumber ??= "";
            if (account is UntrackedAccount or InvestmentAccount)
                return;

            account.AccountNumber = new string(
                account.AccountNumber.Where(char.IsDigit).ToArray()
            );
        }
    }
}
