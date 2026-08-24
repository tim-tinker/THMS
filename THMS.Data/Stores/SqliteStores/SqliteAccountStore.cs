using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteAccountStore
    {
        private readonly AccountTable _accountTable = new();
        private readonly ExternalAccountLinksTable _externalLinkTable = new();
        private readonly BankAccountTable _bankAccountTable = new();
        private readonly CreditAccountTable _creditAccountTable = new();
        private readonly LoanAccountTable _loanAccountTable = new();
        private readonly MortgageAccountTable _mortgageAccountTable = new();
        private readonly InvestmentAccountTable _investmentAccountTable = new();
        private readonly InternalAccountTable _internalAccountTable = new();

        public void InitializeSchema(SqliteConnection conn)
        {
            _accountTable.InitializeSchema(conn);
            _externalLinkTable.InitializeSchema(conn);
            _bankAccountTable.InitializeSchema(conn);
            _creditAccountTable.InitializeSchema(conn);
            _loanAccountTable.InitializeSchema(conn);
            _mortgageAccountTable.InitializeSchema(conn);
            _investmentAccountTable.InitializeSchema(conn);
            _internalAccountTable.InitializeSchema(conn);
        }

        public void Upsert(SqliteConnection conn, Account account)
        {
            _accountTable.Upsert(conn, account);
            UpsertExternalLink(conn, account);

            switch (account)
            {
                case BankAccount bank:
                    _bankAccountTable.Upsert(conn, bank);
                    break;
                case CreditAccount credit:
                    _creditAccountTable.Upsert(conn, credit);
                    break;
                case LoanAccount loan:
                    _loanAccountTable.Upsert(conn, loan);
                    break;
                case MortgageAccount mortgage:
                    _mortgageAccountTable.Upsert(conn, mortgage);
                    break;
                case InvestmentAccount investment:
                    _investmentAccountTable.Upsert(conn, investment);
                    break;
                case InternalAccount internalAccount:
                    _internalAccountTable.Upsert(conn, internalAccount);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown account type: {account.GetType().Name}");
            }
        }

        public Account? Get(SqliteConnection conn, Guid id)
        {
            var baseInfo = _accountTable.GetBase(conn, id);
            if (baseInfo == null)
                return null;

            var (name, institution, accountNumber, type, balanceAsOf, classType) = baseInfo.Value;
            var externalLink = _externalLinkTable.Get(conn, id);

            Account? account = classType switch
            {
                nameof(BankAccount) => LoadBank(conn, id),
                nameof(CreditAccount) => LoadCredit(conn, id),
                nameof(LoanAccount) => LoadLoan(conn, id),
                nameof(MortgageAccount) => LoadMortgage(conn, id),
                nameof(InvestmentAccount) => LoadInvestment(conn, id),
                nameof(InternalAccount) => LoadInternal(conn, id),
                _ => null
            };

            if (account == null)
                return null;

            account.Id = id;
            account.Name = name;
            account.Institution = institution;
            account.AccountNumber = accountNumber;
            account.Type = type;
            account.BalanceAsOf = balanceAsOf;
            account.ExternalLink = externalLink;
            return account;
        }

        public IEnumerable<Account> GetAll(SqliteConnection conn)
        {
            foreach (var id in _accountTable.GetAllIds(conn))
            {
                var account = Get(conn, id);
                if (account != null)
                    yield return account;
            }
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            _externalLinkTable.Delete(conn, id);
            _bankAccountTable.Delete(conn, id);
            _creditAccountTable.Delete(conn, id);
            _loanAccountTable.Delete(conn, id);
            _mortgageAccountTable.Delete(conn, id);
            _investmentAccountTable.Delete(conn, id);
            _internalAccountTable.Delete(conn, id);
            _accountTable.Delete(conn, id);
        }

        private void UpsertExternalLink(SqliteConnection conn, Account account)
        {
            if (account.ExternalLink is null)
                _externalLinkTable.Delete(conn, account.Id);
            else
                _externalLinkTable.Upsert(conn, account.Id, account.ExternalLink);
        }

        private BankAccount? LoadBank(SqliteConnection conn, Guid id)
        {
            var details = _bankAccountTable.Get(conn, id);
            if (details == null)
                return null;

            return new BankAccount
            {
                PostedBalance = details.Value.PostedBalance,
                OverdraftLimit = details.Value.OverdraftLimit
            };
        }

        private CreditAccount? LoadCredit(SqliteConnection conn, Guid id)
        {
            var details = _creditAccountTable.Get(conn, id);
            if (details == null)
                return null;

            return new CreditAccount
            {
                CreditLimit = details.Value.CreditLimit,
                APR = details.Value.APR,
                StatementDate = details.Value.StatementDate,
                DueDate = details.Value.DueDate,
                PostedBalance = details.Value.PostedBalance
            };
        }

        private LoanAccount? LoadLoan(SqliteConnection conn, Guid id)
        {
            var details = _loanAccountTable.Get(conn, id);
            if (details == null)
                return null;

            return new LoanAccount
            {
                Principal = details.Value.Principal,
                InterestRate = details.Value.InterestRate,
                NextPaymentDate = details.Value.NextPaymentDate
            };
        }

        private MortgageAccount? LoadMortgage(SqliteConnection conn, Guid id)
        {
            var details = _mortgageAccountTable.Get(conn, id);
            if (details == null)
                return null;

            return new MortgageAccount
            {
                Principal = details.Value.Principal,
                InterestRate = details.Value.InterestRate,
                TermMonths = details.Value.TermMonths,
                NextPaymentDate = details.Value.NextPaymentDate
            };
        }

        private InvestmentAccount? LoadInvestment(SqliteConnection conn, Guid id)
        {
            var details = _investmentAccountTable.Get(conn, id);
            if (details == null)
                return null;

            return new InvestmentAccount
            {
                CashBalance = details.Value.CashBalance,
                MarketValue = details.Value.MarketValue
            };
        }

        private InternalAccount? LoadInternal(SqliteConnection conn, Guid id)
        {
            var purpose = _internalAccountTable.GetPurpose(conn, id);
            if (purpose == null)
                return null;

            return new InternalAccount
            {
                Purpose = purpose
            };
        }
    }
}
