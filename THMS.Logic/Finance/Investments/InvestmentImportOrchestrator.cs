using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Investments
{
    public sealed class InvestmentImportOrchestrator
    {
        public const string ContributionCategoryName = "Investment Contribution";
        public const string DistributionCategoryName = "Investment Distribution";
        public const string IncomeCategoryName = "Investment Income";

        private readonly IAccountDataStore _accounts;
        private readonly IInvestmentBookStore _books;
        private readonly ITransactionDataStore _transactions;
        private readonly ICategoryDataStore _categories;
        private InvestmentBookState? _pending;
        private Guid _pendingAccountId;
        private List<InvestmentBookState> _pendingPositions = [];

        public InvestmentImportOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetInvestmentBookStore(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetCategoryStore())
        {
        }

        public InvestmentImportOrchestrator(
            IAccountDataStore accounts,
            IInvestmentBookStore books,
            ITransactionDataStore transactions,
            ICategoryDataStore categories)
        {
            _accounts = accounts;
            _books = books;
            _transactions = transactions;
            _categories = categories;
        }

        public InvestmentSnapshot Snapshot(Guid accountId) =>
            InvestmentBookkeeper.Replay(Load(accountId));

        public IReadOnlyList<InvestmentAction> ReviewRows(Guid accountId) =>
            _books.GetActions(accountId)
                .Where(action => action.Status == InvestmentActionStatus.Review)
                .OrderBy(action => action.RunDate)
                .ToList();

        public InvestmentImportPreview Preview(Guid accountId, string? positionsPath, string? historyPath)
        {
            if (string.IsNullOrWhiteSpace(positionsPath) && string.IsNullOrWhiteSpace(historyPath))
                throw new InvalidOperationException("Choose a positions view, a history file, or both.");

            var account = RequireInvestment(accountId);
            var state = Load(accountId);
            var positions = string.IsNullOrWhiteSpace(positionsPath)
                ? null
                : FidelityInvestmentFile.ReadPositions(positionsPath);
            var history = string.IsNullOrWhiteSpace(historyPath)
                ? null
                : FidelityInvestmentFile.ReadHistory(historyPath);
            var preview = InvestmentBookkeeper.Import(state, positions, history);
            if (positions is not null && !SameAccount(account.AccountNumber, positions.AccountNumber))
            {
                preview = new InvestmentImportPreview
                {
                    Applied = preview.Applied,
                    Skipped = preview.Skipped,
                    Updated = preview.Updated,
                    Review = preview.Review,
                    Changes = preview.Changes,
                    Snapshot = preview.Snapshot,
                    Messages = preview.Messages
                        .Append($"The file is for account {positions.AccountNumber}. This account is {account.AccountNumber}.")
                        .ToList()
                };
            }

            _pending = state;
            _pendingAccountId = accountId;
            return preview;
        }

        public InvestmentPositionsBatch PreviewPositions(string? positionsPath)
        {
            if (string.IsNullOrWhiteSpace(positionsPath))
                throw new InvalidOperationException("Choose a positions view.");

            var investments = _accounts.GetAllAccounts().OfType<InvestmentAccount>().ToList();
            var matched = new List<InvestmentBookState>();
            var names = new Dictionary<Guid, string>();
            var changes = new List<InvestmentAction>();
            var messages = new List<string>();
            var applied = 0;
            var skipped = 0;
            var updated = 0;
            foreach (var file in FidelityInvestmentFile.ReadPositionAccounts(positionsPath))
            {
                var account = investments.FirstOrDefault(item => AccountNumbersMatch(item.AccountNumber, file.AccountNumber));
                var label = string.IsNullOrWhiteSpace(file.AccountName)
                    ? file.AccountNumber
                    : $"{file.AccountName} ({file.AccountNumber})";
                if (account is null)
                {
                    messages.Add($"No portfolio has account number {label}.");
                    continue;
                }

                var state = Load(account.Id);
                var preview = InvestmentBookkeeper.Import(state, file, history: null);
                matched.Add(state);
                names[account.Id] = account.Name ?? label;
                changes.AddRange(preview.Changes);
                applied += preview.Applied;
                skipped += preview.Skipped;
                updated += preview.Updated;
                messages.Add($"{account.Name}: {string.Join(" ", preview.Messages)}".Trim());
            }

            _pendingPositions = matched;
            _pending = null;
            return new InvestmentPositionsBatch
            {
                Applied = applied,
                Skipped = skipped,
                Updated = updated,
                Messages = messages,
                Changes = changes,
                AccountNames = names
            };
        }

        public void Commit(Guid accountId)
        {
            if (_pending is null || _pendingAccountId != accountId)
                throw new InvalidOperationException("Preview the files before importing them.");

            Save(_pending);
            _pending = null;
        }

        public void CommitPositions()
        {
            if (_pendingPositions.Count == 0)
                throw new InvalidOperationException("Preview the positions file before importing it.");

            foreach (var state in _pendingPositions)
                Save(state);
            _pendingPositions = [];
        }

        public void Classify(Guid accountId, Guid actionId, InvestmentActionKind kind)
        {
            var state = Load(accountId);
            var message = InvestmentBookkeeper.Classify(state, actionId, kind);
            if (message is not null)
                throw new InvalidOperationException(message);
            Save(state);
        }

        private void Save(InvestmentBookState state)
        {
            var account = RequireInvestment(state.AccountId);
            foreach (var security in state.Securities)
                _books.UpsertSecurity(security);
            foreach (var quote in state.Quotes)
                _books.UpsertQuote(quote);
            PostLedger(state.Actions);
            foreach (var action in state.Actions)
                _books.UpsertAction(action);

            var snapshot = InvestmentBookkeeper.Replay(state);
            account.TracksPositions = state.OpenedOn is not null;
            account.BookOpenedOn = state.OpenedOn;
            if (account.TracksPositions)
            {
                account.CashBalance = decimal.Round(snapshot.Cash, 2);
                account.PositionValue = decimal.Round(snapshot.PositionValue, 2);
                account.BalanceAsOf = LatestDate(state);
            }

            _accounts.UpsertAccount(account);
        }

        private void PostLedger(IEnumerable<InvestmentAction> actions)
        {
            foreach (var action in actions)
            {
                if (action.LedgerTransactionId is not null)
                    continue;
                if (action.Status != InvestmentActionStatus.Applied)
                    continue;
                if (action.Amount is not decimal amount || amount == 0)
                    continue;

                var categoryName = action.Kind switch
                {
                    InvestmentActionKind.Contribution => ContributionCategoryName,
                    InvestmentActionKind.Distribution => DistributionCategoryName,
                    InvestmentActionKind.Dividend or InvestmentActionKind.Interest => IncomeCategoryName,
                    _ => null
                };
                if (categoryName is null)
                    continue;
                var posted = new PostedTransaction
                {
                    AccountId = action.AccountId,
                    Date = action.RunDate.Date,
                    Amount = amount,
                    Description = string.IsNullOrWhiteSpace(action.ActionText) ? categoryName : action.ActionText.Trim(),
                    ImportedStatus = ImportedStatus.Matched,
                    ExternalTransactionId = "investment:" + action.Id
                };
                posted.ApplyCategory(EnsureCategory(categoryName));
                _transactions.AddPostedTransaction(posted);
                action.LedgerTransactionId = posted.Id;
            }
        }

        private ExpenseCategory EnsureCategory(string name)
        {
            _categories.EnsureDefaultCategories();
            var existing = _categories.GetAllCategories(includeInactive: true)
                .FirstOrDefault(category => string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new ExpenseCategory
                {
                    Name = name,
                    IsActive = true,
                    DisplayOrder = 60,
                    Role = CategoryRole.Investment
                };
                _categories.AddCategory(existing);
                return existing;
            }

            if (existing.Role != CategoryRole.Investment)
            {
                existing.Role = CategoryRole.Investment;
                _categories.UpdateCategory(existing);
            }

            return existing;
        }

        private InvestmentBookState Load(Guid accountId)
        {
            var account = RequireInvestment(accountId);
            var state = new InvestmentBookState
            {
                AccountId = accountId,
                OpenedOn = account.BookOpenedOn
            };
            state.Actions.AddRange(_books.GetActions(accountId));
            state.Quotes.AddRange(_books.GetQuotes(accountId));
            state.Securities.AddRange(_books.GetSecurities());
            return state;
        }

        private InvestmentAccount RequireInvestment(Guid accountId)
        {
            if (_accounts.GetAllAccounts().FirstOrDefault(account => account.Id == accountId) is not InvestmentAccount investment)
                throw new InvalidOperationException("Choose an investment account.");
            return investment;
        }

        private static DateTime LatestDate(InvestmentBookState state)
        {
            DateTime? latest = state.OpenedOn;
            foreach (var action in state.Actions)
            {
                if (action.Status == InvestmentActionStatus.Applied && (latest is null || action.RunDate > latest))
                    latest = action.RunDate;
            }

            foreach (var quote in state.Quotes)
            {
                if (latest is null || quote.AsOf > latest)
                    latest = quote.AsOf;
            }

            return latest ?? DateTime.Today;
        }

        private static bool SameAccount(string? stored, string? file)
        {
            if (string.IsNullOrWhiteSpace(stored) || string.IsNullOrWhiteSpace(file))
                return true;
            return AccountNumbersMatch(stored, file)
                || file.Trim().EndsWith(stored.Trim(), StringComparison.OrdinalIgnoreCase)
                || stored.Trim().EndsWith(file.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool AccountNumbersMatch(string? stored, string? file)
        {
            if (string.IsNullOrWhiteSpace(stored) || string.IsNullOrWhiteSpace(file))
                return false;
            return string.Equals(stored.Trim(), file.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class InvestmentPositionsBatch
    {
        public int Applied { get; init; }
        public int Skipped { get; init; }
        public int Updated { get; init; }
        public IReadOnlyList<string> Messages { get; init; } = [];
        public IReadOnlyList<InvestmentAction> Changes { get; init; } = [];
        public IReadOnlyDictionary<Guid, string> AccountNames { get; init; } = new Dictionary<Guid, string>();
    }
}
