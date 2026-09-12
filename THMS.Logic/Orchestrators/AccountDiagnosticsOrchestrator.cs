using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Planning;

namespace THMS.Logic.Orchestrators.Finance
{
    public class AccountDiagnosticsOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly IAccountStatementDataStore _statements;

        public AccountDiagnosticsOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetAccountStatementStore())
        {
        }

        public AccountDiagnosticsOrchestrator(IAccountDataStore accounts, ITransactionDataStore transactions)
            : this(accounts, transactions, new AccountStatementDataStore())
        {
        }

        public AccountDiagnosticsOrchestrator(
            IAccountDataStore accounts,
            ITransactionDataStore transactions,
            IAccountStatementDataStore statements)
        {
            _accounts = accounts;
            _transactions = transactions;
            _statements = statements;
        }

        public List<string> Run(IEnumerable<string>? spreadsheetNames = null)
        {
            var findings = new List<string>();
            var accounts = _accounts.GetAllAccounts().ToList();
            var accountIds = accounts.Select(a => a.Id).ToHashSet();

            FindMissingMetadata(accounts, findings);
            FindDuplicates(accounts, findings);
            FindTransactionCoverage(accounts, accountIds, findings);
            FindNameMismatches(accounts, spreadsheetNames, findings);
            FindStatementIssues(accounts, findings);

            if (findings.Count == 0)
                findings.Add("No issues found.");

            return findings;
        }

        private static void FindMissingMetadata(IReadOnlyList<Account> accounts, List<string> findings)
        {
            foreach (var credit in accounts.OfType<CreditAccount>())
            {
                if (credit.APR <= 0)
                    findings.Add($"Missing APR: {credit.Name}");
                if (credit.CreditLimit <= 0)
                    findings.Add($"Missing credit limit: {credit.Name}");
            }

            foreach (var loan in accounts.OfType<LoanAccount>())
            {
                if (loan.TermMonths <= 0)
                    findings.Add($"Missing loan term: {loan.Name}");
            }

            foreach (var mortgage in accounts.OfType<MortgageAccount>())
            {
                var missing = new List<string>();
                if (mortgage.Principal <= 0)
                    missing.Add("principal");
                if (mortgage.InterestRate <= 0)
                    missing.Add("interest rate");
                if (mortgage.TermMonths <= 0)
                    missing.Add("term");
                if (mortgage.NextPaymentDate == default)
                    missing.Add("next payment date");
                if (missing.Count > 0)
                    findings.Add($"Missing mortgage metadata ({string.Join(", ", missing)}): {mortgage.Name}");
            }
        }

        private static void FindDuplicates(IReadOnlyList<Account> accounts, List<string> findings)
        {
            foreach (var group in accounts.GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                         .Where(g => g.Count() > 1))
                findings.Add($"Duplicate accounts: {group.Key} ({group.Count()})");

            foreach (var group in accounts
                         .Where(a => !string.IsNullOrWhiteSpace(a.AccountNumber))
                         .GroupBy(a => a.AccountNumber, StringComparer.Ordinal)
                         .Where(g => g.Select(a => a.Id).Distinct().Count() > 1))
                findings.Add($"Duplicate account number {group.Key}: {string.Join(", ", group.Select(a => a.Name))}");
        }

        private void FindTransactionCoverage(
            IReadOnlyList<Account> accounts,
            HashSet<Guid> accountIds,
            List<string> findings)
        {
            var posted = _transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var transfers = _transactions.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var futureSingles = _transactions.GetAllFutureSingleTransactions().ToList();
            var futureTransfers = _transactions.GetAllFutureTransferTransactions().ToList();

            foreach (var transaction in posted.Where(t => !accountIds.Contains(t.AccountId)))
                findings.Add($"Orphaned transaction {transaction.Description} ({transaction.Date:d})");
            foreach (var transaction in transfers.Where(t => !accountIds.Contains(t.AccountId)))
                findings.Add($"Orphaned transfer {transaction.Description} ({transaction.Date:d})");
            foreach (var transaction in futureSingles.Where(t => !accountIds.Contains(t.AccountId)))
                findings.Add($"Orphaned future transaction {transaction.Description} ({transaction.Date:d})");
            foreach (var transaction in futureTransfers.Where(t =>
                         !accountIds.Contains(t.FromAccountId) || !accountIds.Contains(t.ToAccountId)))
                findings.Add($"Orphaned future transfer {transaction.Description} ({transaction.Date:d})");

            var usedIds = posted.Select(t => t.AccountId)
                .Concat(transfers.Select(t => t.AccountId))
                .Concat(futureSingles.Select(t => t.AccountId))
                .Concat(futureTransfers.Select(t => t.FromAccountId))
                .Concat(futureTransfers.Select(t => t.ToAccountId))
                .ToHashSet();

            foreach (var account in accounts.Where(a => !usedIds.Contains(a.Id)))
                findings.Add($"Account with no transactions: {account.Name}");
        }

        private static void FindNameMismatches(
            IReadOnlyList<Account> accounts,
            IEnumerable<string>? spreadsheetNames,
            List<string> findings)
        {
            var names = spreadsheetNames?
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (names is null || names.Count == 0)
                return;

            var ledger = accounts.Select(a => a.Name).ToList();
            var ledgerSet = ledger.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var name in names.Where(n => !ledgerSet.Contains(n)))
                findings.Add($"Name mismatch: spreadsheet '{name}' is not in the ledger");

            foreach (var ledgerName in ledger)
            {
                if (names.Contains(ledgerName, StringComparer.OrdinalIgnoreCase))
                    continue;
                var close = names.FirstOrDefault(n =>
                    string.Equals(Normalize(n), Normalize(ledgerName), StringComparison.OrdinalIgnoreCase));
                if (close is not null)
                    findings.Add($"Name mismatch: spreadsheet '{close}' vs ledger '{ledgerName}'");
            }
        }

        private static string Normalize(string value) =>
            new(value.Where(char.IsLetterOrDigit).ToArray());

        private void FindStatementIssues(IReadOnlyList<Account> accounts, List<string> findings)
        {
            foreach (var account in accounts)
            {
                var statements = _statements.GetForAccount(account.Id);
                if (NeedsStatement(account) && statements.Count == 0)
                    findings.Add($"Missing statement: {account.Name}");

                foreach (var statement in statements)
                {
                    foreach (var issue in AccountStatementValidator.Validate(statement))
                        findings.Add($"Invalid {statement.Type} statement for {account.Name}: {issue}");
                }

                var latest = statements.OrderByDescending(s => s.StatementDate).FirstOrDefault();
                if (latest is not null)
                    FindMismatchedBalance(account, latest, findings);
            }
        }

        private static bool NeedsStatement(Account account) =>
            account is CreditAccount or LoanAccount or MortgageAccount;

        private static void FindMismatchedBalance(Account account, AccountStatement statement, List<string> findings)
        {
            switch (statement)
            {
                case BankStatement bank when account is BankAccount bankAccount:
                    if (Math.Abs(bank.EndingBalance - bankAccount.PostedBalance) > AccountStatementValidator.SumTolerance)
                        findings.Add($"Mismatched statement balance for {account.Name}: statement {bank.EndingBalance:c2} vs ledger {bankAccount.PostedBalance:c2}");
                    break;
                case CreditCardStatement card when account is CreditAccount credit:
                    var owed = PostedBalanceCalculator.ToDisplayBalance(credit, credit.PostedBalance);
                    if (Math.Abs(card.StatementBalance - owed) > AccountStatementValidator.SumTolerance)
                        findings.Add($"Mismatched statement balance for {account.Name}: statement {card.StatementBalance:c2} vs ledger {owed:c2}");
                    break;
                case LoanStatement loan when account is LoanAccount loanAccount:
                    if (Math.Abs(loan.PrincipalBalance - loanAccount.Principal) > AccountStatementValidator.SumTolerance)
                        findings.Add($"Mismatched principal for {account.Name}: statement {loan.PrincipalBalance:c2} vs account {loanAccount.Principal:c2}");
                    break;
                case MortgageStatement mortgage when account is MortgageAccount mortgageAccount:
                    if (Math.Abs(mortgage.PrincipalBalance - mortgageAccount.Principal) > AccountStatementValidator.SumTolerance)
                        findings.Add($"Mismatched principal for {account.Name}: statement {mortgage.PrincipalBalance:c2} vs account {mortgageAccount.Principal:c2}");
                    break;
            }
        }
    }
}
