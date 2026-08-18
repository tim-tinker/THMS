using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteAccountStore
    {
        private readonly AccountTable _accountTable = new();
        private readonly BankAccountTable _bankAccountTable = new();
        private readonly CreditAccountTable _creditAccountTable = new();
        private readonly LoanAccountTable _loanAccountTable = new();
        private readonly MortgageAccountTable _mortgageAccountTable = new();
        private readonly InvestmentAccountTable _investmentAccountTable = new();
        private readonly InternalAccountTable _internalAccountTable = new();

        public void InitializeSchema(SqliteConnection conn)
        {
            _accountTable.InitializeSchema(conn);
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

            switch (classType)
            {
                case nameof(BankAccount):
                {
                    var details = _bankAccountTable.Get(conn, id);
                    if (details == null)
                        return null;

                    return new BankAccount
                    {
                        Id = id,
                        Name = name,
                        Institution = institution,
                        AccountNumber = accountNumber,
                        Type = type,
                        BalanceAsOf = balanceAsOf,
                        PostedBalance = details.Value.PostedBalance,
                        OverdraftLimit = details.Value.OverdraftLimit
                    };
                }

                case nameof(CreditAccount):
                {
                    var details = _creditAccountTable.Get(conn, id);
                    if (details == null)
                        return null;

                    return new CreditAccount
                    {
                        Id = id,
                        Name = name,
                        Institution = institution,
                        AccountNumber = accountNumber,
                        Type = type,
                        BalanceAsOf = balanceAsOf,
                        CreditLimit = details.Value.CreditLimit,
                        APR = details.Value.APR,
                        StatementDate = details.Value.StatementDate,
                        DueDate = details.Value.DueDate,
                        PostedBalance = details.Value.PostedBalance
                    };
                }

                case nameof(LoanAccount):
                {
                    var details = _loanAccountTable.Get(conn, id);
                    if (details == null)
                        return null;

                    return new LoanAccount
                    {
                        Id = id,
                        Name = name,
                        Institution = institution,
                        AccountNumber = accountNumber,
                        Type = type,
                        BalanceAsOf = balanceAsOf,
                        Principal = details.Value.Principal,
                        InterestRate = details.Value.InterestRate,
                        NextPaymentDate = details.Value.NextPaymentDate
                    };
                }

                case nameof(MortgageAccount):
                {
                    var details = _mortgageAccountTable.Get(conn, id);
                    if (details == null)
                        return null;

                    return new MortgageAccount
                    {
                        Id = id,
                        Name = name,
                        Institution = institution,
                        AccountNumber = accountNumber,
                        Type = type,
                        BalanceAsOf = balanceAsOf,
                        Principal = details.Value.Principal,
                        InterestRate = details.Value.InterestRate,
                        TermMonths = details.Value.TermMonths,
                        NextPaymentDate = details.Value.NextPaymentDate
                    };
                }

                case nameof(InvestmentAccount):
                {
                    var details = _investmentAccountTable.Get(conn, id);
                    if (details == null)
                        return null;

                    return new InvestmentAccount
                    {
                        Id = id,
                        Name = name,
                        Institution = institution,
                        AccountNumber = accountNumber,
                        Type = type,
                        BalanceAsOf = balanceAsOf,
                        CashBalance = details.Value.CashBalance,
                        MarketValue = details.Value.MarketValue
                    };
                }

                case nameof(InternalAccount):
                {
                    var purpose = _internalAccountTable.GetPurpose(conn, id);
                    if (purpose == null)
                        return null;

                    return new InternalAccount
                    {
                        Id = id,
                        Name = name,
                        Institution = institution,
                        AccountNumber = accountNumber,
                        Type = type,
                        BalanceAsOf = balanceAsOf,
                        Purpose = purpose
                    };
                }

                default:
                    return null;
            }
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
    }
}
