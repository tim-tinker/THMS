using THMS.Data.Stores;
using THMS.Domain.Finance.Planning;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators
{
    public class StatementImportOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly IAccountStatementDataStore _statements;
        private readonly PlanningOrchestrator _planning;
        private readonly SpreadsheetStatementImporter _importer;

        public StatementImportOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetAccountStatementStore())
        {
        }

        public StatementImportOrchestrator(IAccountDataStore accounts, IAccountStatementDataStore statements)
            : this(
                accounts,
                statements,
                new PlanningOrchestrator(accounts, new DataStoreFactory().GetTransactionStore(), statements),
                new SpreadsheetStatementImporter(accounts))
        {
        }

        public StatementImportOrchestrator(
            IAccountDataStore accounts,
            IAccountStatementDataStore statements,
            ITransactionDataStore transactions)
            : this(
                accounts,
                statements,
                new PlanningOrchestrator(accounts, transactions, statements),
                new SpreadsheetStatementImporter(accounts))
        {
        }

        public StatementImportOrchestrator(
            IAccountDataStore accounts,
            IAccountStatementDataStore statements,
            PlanningOrchestrator planning,
            SpreadsheetStatementImporter importer)
        {
            _accounts = accounts;
            _statements = statements;
            _planning = planning;
            _importer = importer;
        }

        public List<StatementImportPreview> LoadStatementsFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A file path is required.");
            if (!File.Exists(path))
                throw new FileNotFoundException("The selected file was not found.", path);

            return _importer.Parse(path).Select(ToPreview).ToList();
        }

        public ImportResult ImportStatements(IEnumerable<StatementImportPreview> previewRows) =>
            ImportStatements(previewRows, progress: null);

        public ImportResult ImportStatements(
            IEnumerable<StatementImportPreview> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var rows = previewRows as IReadOnlyList<StatementImportPreview> ?? previewRows.ToList();
            var existingByAccount = new Dictionary<Guid, List<AccountStatement>>();
            var imported = new List<StatementImportPreview>();
            ImportProgressReporter.Report(progress, 0, rows.Count, stride: 1);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (TryImport(row, existingByAccount))
                    imported.Add(row);

                ImportProgressReporter.Report(progress, i + 1, rows.Count, stride: 1);
            }

            return ImportResult.FromDates(imported.Count, imported.Select(row => row.StatementDate));
        }

        private bool TryImport(
            StatementImportPreview row,
            Dictionary<Guid, List<AccountStatement>> existingByAccount)
        {
            if (row.AccountId == Guid.Empty || _accounts.GetAllAccounts().All(a => a.Id != row.AccountId))
                return false;

            var statement = ToStatement(row);
            if (statement is null)
                return false;
            if (AccountStatementValidator.Validate(statement).Count > 0)
                return false;
            if (IsDuplicate(statement, existingByAccount))
                return false;

            _planning.SaveStatement(statement);
            if (statement is not BankStatement && statement.AmountDue > 0)
            {
                var payFromId = row.PayFromAccountId
                    ?? ResolvePayFrom(row.PayFrom, statement.AccountId);
                if (payFromId is Guid payFrom)
                {
                    try
                    {
                        _planning.EnsureStatementPayment(statement, payFrom);
                    }
                    catch (InvalidOperationException)
                    {
                        // Keep the statement even if a pay-from account cannot fund it.
                    }
                }
            }

            existingByAccount[statement.AccountId].Add(statement);
            return true;
        }

        private static StatementImportPreview ToPreview(ParsedSpreadsheetStatement parsed) =>
            new()
            {
                Account = parsed.AccountName,
                Type = AccountStatementListRow.DisplayType(parsed.Type),
                StatementDate = parsed.StatementDate,
                DueDate = parsed.DueDate,
                AmountDue = parsed.AmountDue,
                StatementBalance = parsed.StatementBalance,
                EscrowBalance = parsed.EscrowBalance,
                Notes = parsed.Notes ?? "",
                PayFrom = parsed.PayFromName ?? "",
                AccountId = parsed.AccountId,
                PayFromAccountId = parsed.PayFromAccountId,
                StatementType = parsed.Type
            };

        private static AccountStatement? ToStatement(StatementImportPreview row)
        {
            AccountStatement statement;
            try
            {
                statement = StatementAccountMatch.CreateStatement(row.StatementType);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            statement.AccountId = row.AccountId;
            statement.StatementDate = row.StatementDate.Date;
            statement.Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim();

            if (statement is BankStatement bank)
            {
                bank.DueDate = bank.StatementDate;
                bank.AmountDue = 0;
                bank.StatementBalance = row.StatementBalance;
                return bank;
            }

            statement.DueDate = row.DueDate == default ? statement.StatementDate : row.DueDate.Date;
            statement.AmountDue = row.AmountDue;

            switch (statement)
            {
                case LoanStatement loan:
                    loan.StatementBalance = row.StatementBalance;
                    break;
                case MortgageStatement mortgage:
                    mortgage.StatementBalance = row.StatementBalance;
                    mortgage.EscrowBalance = row.EscrowBalance;
                    break;
                case CreditCardStatement card:
                    card.StatementBalance = row.StatementBalance;
                    break;
                case UtilityStatement utility when row.AmountDue > 0:
                    utility.Charges =
                    [
                        new UtilityChargeLine { Description = "Amount due", Amount = row.AmountDue }
                    ];
                    break;
                case ServiceStatement service when row.AmountDue > 0:
                    service.Charges =
                    [
                        new ServiceChargeLine { Description = "Amount due", Amount = row.AmountDue }
                    ];
                    break;
            }

            return statement;
        }

        private bool IsDuplicate(
            AccountStatement statement,
            Dictionary<Guid, List<AccountStatement>> existingByAccount)
        {
            if (!existingByAccount.TryGetValue(statement.AccountId, out var existing))
            {
                existing = _statements.GetForAccount(statement.AccountId);
                existingByAccount[statement.AccountId] = existing;
            }

            return existing.Any(item => item.StatementDate.Date == statement.StatementDate.Date);
        }

        private Guid? ResolvePayFrom(string? name, Guid statementAccountId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var trimmed = name.Trim();
            var account = _accounts.GetAccount(trimmed)
                ?? _accounts.GetAllAccounts().FirstOrDefault(a =>
                    string.Equals(a.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            if (account is null || account.Id == statementAccountId)
                return null;
            return account.Id;
        }
    }
}
