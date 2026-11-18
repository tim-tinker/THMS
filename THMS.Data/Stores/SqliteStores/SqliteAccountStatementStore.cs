using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteAccountStatementStore
    {
        private readonly AccountStatementsTable _statements = new();
        private readonly AccountStatementDetailsTable _details = new();
        private readonly PromotionalBalancesTable _promotions = new();
        private readonly UtilityUsageRecordsTable _usage = new();
        private readonly StatementChargeLinesTable _charges = new();
        private readonly PropertyTaxStatementsTable _propertyTax = new();

        public void InitializeSchema(SqliteConnection conn)
        {
            _statements.InitializeSchema(conn);
            _details.InitializeSchema(conn);
            _promotions.InitializeSchema(conn);
            _usage.InitializeSchema(conn);
            _charges.InitializeSchema(conn);
            _propertyTax.InitializeSchema(conn);
        }

        public void Save(SqliteConnection conn, AccountStatement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);
            if (statement.Id == Guid.Empty)
                statement.Id = Guid.NewGuid();

            _statements.Upsert(conn, statement);
            _details.Upsert(conn, statement.Id, statement);
            ReplaceChildren(conn, statement);
        }

        public AccountStatement? Get(SqliteConnection conn, Guid id)
        {
            var row = _statements.Get(conn, id);
            return row is null ? null : Hydrate(conn, row);
        }

        public List<AccountStatement> GetForAccount(SqliteConnection conn, Guid accountId) =>
            HydrateAll(conn, _statements.GetForAccount(conn, accountId));

        public List<AccountStatement> GetUpcoming(SqliteConnection conn, DateTime asOf) =>
            HydrateAll(conn, _statements.GetUpcoming(conn, asOf));

        public List<AccountStatement> GetForAsset(SqliteConnection conn, Guid assetId)
        {
            var list = new List<AccountStatement>();
            foreach (var id in _propertyTax.GetStatementIds(conn, assetId))
            {
                var statement = Get(conn, id);
                if (statement is not null)
                    list.Add(statement);
            }

            return list
                .OrderByDescending(statement => statement is PropertyTaxStatement tax ? tax.TaxYear : 0)
                .ToList();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            _promotions.DeleteByStatement(conn, id);
            _usage.DeleteByStatement(conn, id);
            _charges.DeleteByStatement(conn, id);
            _propertyTax.Delete(conn, id);
            _details.Delete(conn, id);
            _statements.Delete(conn, id);
        }

        private void ReplaceChildren(SqliteConnection conn, AccountStatement statement)
        {
            _promotions.DeleteByStatement(conn, statement.Id);
            _usage.DeleteByStatement(conn, statement.Id);
            _charges.DeleteByStatement(conn, statement.Id);
            _propertyTax.Delete(conn, statement.Id);

            switch (statement)
            {
                case CreditCardStatement card:
                    _promotions.ReplaceAll(conn, statement.Id, statement.AccountId, card.Promotions);
                    break;
                case UtilityStatement utility:
                    _usage.ReplaceAll(conn, statement.Id, utility.Usage);
                    _charges.ReplaceUtility(conn, statement.Id, utility.Charges);
                    break;
                case ServiceStatement service:
                    _charges.ReplaceService(conn, statement.Id, service.Charges);
                    break;
                case PropertyTaxStatement tax:
                    _propertyTax.Upsert(conn, tax);
                    break;
            }
        }

        private List<AccountStatement> HydrateAll(SqliteConnection conn, IEnumerable<AccountStatementRow> rows)
        {
            var list = new List<AccountStatement>();
            foreach (var row in rows)
            {
                var statement = Hydrate(conn, row);
                if (statement is not null)
                    list.Add(statement);
            }

            return list;
        }

        private AccountStatement? Hydrate(SqliteConnection conn, AccountStatementRow row)
        {
            if (!Enum.TryParse<StatementType>(row.StatementType, out var type))
                return null;

            var details = _details.Get(conn, row.Id);
            AccountStatement? statement = type switch
            {
                StatementType.Bank => new BankStatement
                {
                    StatementBalance = details.StatementBalance
                },
                StatementType.Investment => new InvestmentStatement
                {
                    StatementBalance = details.StatementBalance
                },
                StatementType.Loan => new LoanStatement
                {
                    StatementBalance = details.StatementBalance
                },
                StatementType.Mortgage => new MortgageStatement
                {
                    StatementBalance = details.StatementBalance,
                    EscrowBalance = details.EscrowBalance
                },
                StatementType.CreditCard => new CreditCardStatement
                {
                    StatementBalance = details.StatementBalance,
                    Promotions = _promotions.GetByStatement(conn, row.Id)
                },
                StatementType.Utility => new UtilityStatement
                {
                    StatementBalance = details.StatementBalance,
                    Usage = _usage.GetByStatement(conn, row.Id),
                    Charges = _charges.GetUtility(conn, row.Id)
                },
                StatementType.Service => new ServiceStatement
                {
                    Charges = _charges.GetService(conn, row.Id)
                },
                StatementType.Insurance => new InsuranceStatement(),
                StatementType.PropertyTax => _propertyTax.Load(conn, row.Id),
                _ => null
            };

            if (statement is null)
                return null;

            statement.Id = row.Id;
            statement.AccountId = row.AccountId;
            statement.StatementDate = row.StatementDate;
            statement.DueDate = row.DueDate;
            statement.AmountDue = row.AmountDue;
            statement.BillCleared = row.BillCleared;
            statement.Notes = row.Notes;
            return statement;
        }
    }
}
