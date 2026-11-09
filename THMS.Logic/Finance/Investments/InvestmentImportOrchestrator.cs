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

        public InvestmentPositionColumnMap? GetPositionMap(Guid accountId) =>
            accountId == Guid.Empty ? null : _accounts.GetInvestmentPositionColumnMap(accountId);

        public void SavePositionMap(InvestmentPositionColumnMap map) =>
            _accounts.UpsertInvestmentPositionColumnMap(map);

        public InvestmentHistoryColumnMap? GetHistoryMap(Guid accountId) =>
            accountId == Guid.Empty ? null : _accounts.GetInvestmentHistoryColumnMap(accountId);

        public void SaveHistoryMap(InvestmentHistoryColumnMap map) =>
            _accounts.UpsertInvestmentHistoryColumnMap(map);

        public IReadOnlyList<InvestmentAccount> InvestmentAccounts() =>
            _accounts.GetAllAccounts().OfType<InvestmentAccount>().OrderBy(account => account.Name).ToList();

        public InvestmentImportPreview Preview(
            Guid accountId,
            string? positionsPath,
            string? historyPath,
            InvestmentPositionColumnMap? positionMap = null,
            InvestmentHistoryColumnMap? historyMap = null)
        {
            if (string.IsNullOrWhiteSpace(positionsPath) && string.IsNullOrWhiteSpace(historyPath))
                throw new InvalidOperationException("Choose a positions view, a history file, or both.");

            var account = RequireInvestment(accountId);
            var state = Load(accountId);
            var positions = ReadPositions(account, positionsPath, positionMap);
            var history = string.IsNullOrWhiteSpace(historyPath)
                ? null
                : historyMap is null
                    ? FidelityInvestmentFile.ReadHistory(historyPath)
                    : InvestmentHistoryMapper.Parse(FidelityInvestmentFile.ReadSheet(historyPath), historyMap);
            if (historyMap is not null)
                SaveHistoryMap(historyMap.ForAccount(accountId));
            if (positionMap is not null)
                SavePositionMap(positionMap.ForAccount(accountId));
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

        public InvestmentPositionsBatch PreviewPositions(string? positionsPath) =>
            PreviewPositions(positionsPath, map: null, targetAccountId: null);

        public InvestmentPositionsBatch PreviewPositions(string? positionsPath, InvestmentPositionColumnMap? map, Guid? targetAccountId)
        {
            if (string.IsNullOrWhiteSpace(positionsPath))
                throw new InvalidOperationException("Choose a positions view.");

            var files = map is null
                ? FidelityInvestmentFile.ReadPositionAccounts(positionsPath)
                : InvestmentPositionMapper.Parse(FidelityInvestmentFile.ReadSheet(positionsPath), map, Path.GetFileName(positionsPath));
            var investments = _accounts.GetAllAccounts().OfType<InvestmentAccount>().ToList();
            InvestmentAccount? target = null;
            if (targetAccountId is Guid targetId)
            {
                target = investments.FirstOrDefault(item => item.Id == targetId);
                if (target is null)
                    throw new InvalidOperationException("Choose an investment account.");
            }
            var matched = new List<InvestmentBookState>();
            var names = new Dictionary<Guid, string>();
            var changes = new List<InvestmentAction>();
            var messages = new List<string>();
            var applied = 0;
            var skipped = 0;
            var updated = 0;
            foreach (var file in files)
            {
                var account = target ?? investments.FirstOrDefault(item => AccountNumbersMatch(item.AccountNumber, file.AccountNumber));
                var label = string.IsNullOrWhiteSpace(file.AccountName)
                    ? file.AccountNumber
                    : $"{file.AccountName} ({file.AccountNumber})";
                if (account is null)
                {
                    messages.Add(string.IsNullOrWhiteSpace(file.AccountNumber)
                        ? "This file has no account number. Choose the portfolio in the column mapping."
                        : $"No portfolio has account number {label}.");
                    continue;
                }

                if (target is not null
                    && !string.IsNullOrWhiteSpace(file.AccountNumber)
                    && !NumberMatches(target.AccountNumber, file.AccountNumber))
                {
                    messages.Add($"{label} was left out. This import is for {target.Name}.");
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
                if (map is not null)
                    SavePositionMap(map.ForAccount(account.Id));
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
            RefreshQuotedValues();
            _pending = null;
        }

        public void CommitPositions()
        {
            if (_pendingPositions.Count == 0)
                throw new InvalidOperationException("Preview the positions file before importing it.");

            foreach (var state in _pendingPositions)
                Save(state);
            RefreshQuotedValues();
            _pendingPositions = [];
        }

        public void Classify(Guid accountId, Guid actionId, InvestmentActionKind kind)
        {
            var state = Load(accountId);
            var message = InvestmentBookkeeper.Classify(state, actionId, kind);
            if (message is not null)
                throw new InvalidOperationException(message);
            Save(state);
            RefreshQuotedValues();
        }

        private void Save(InvestmentBookState state)
        {
            var account = RequireInvestment(state.AccountId);
            foreach (var security in state.Securities)
                _books.UpsertSecurity(security);
            foreach (var quote in state.Quotes)
                _books.UpsertQuote(quote);
            foreach (var quote in state.MarketQuotes)
                _books.UpsertMarketQuote(quote);
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
            state.MarketQuotes.AddRange(_books.GetMarketQuotes());
            state.Securities.AddRange(_books.GetSecurities());
            return state;
        }

        private void RefreshQuotedValues()
        {
            foreach (var account in _accounts.GetAllAccounts().OfType<InvestmentAccount>().ToList())
            {
                var state = Load(account.Id);
                if (state.OpenedOn is null && !state.Actions.Any(action => action.Status == InvestmentActionStatus.Applied))
                    continue;

                var snapshot = InvestmentBookkeeper.Replay(state);
                account.CashBalance = decimal.Round(snapshot.Cash, 2);
                account.PositionValue = decimal.Round(snapshot.PositionValue, 2);
                _accounts.UpsertAccount(account);
            }
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

        private static bool NumberMatches(string? stored, string? file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return true;
            if (string.IsNullOrWhiteSpace(stored))
                return false;
            return AccountNumbersMatch(stored, file)
                || file.Trim().EndsWith(stored.Trim(), StringComparison.OrdinalIgnoreCase)
                || stored.Trim().EndsWith(file.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static FidelityPositionsFile? ReadPositions(InvestmentAccount account, string? path, InvestmentPositionColumnMap? map)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            if (map is null)
                return FidelityInvestmentFile.ReadPositions(path);

            var files = InvestmentPositionMapper.Parse(
                FidelityInvestmentFile.ReadSheet(path),
                map,
                Path.GetFileName(path));
            var chosen = files.Where(file =>
                string.IsNullOrWhiteSpace(file.AccountNumber)
                || NumberMatches(account.AccountNumber, file.AccountNumber)).ToList();
            if (chosen.Count == 0)
                chosen = files.Take(1).ToList();
            if (chosen.Count == 0)
                return new FidelityPositionsFile();
            if (chosen.Count == 1)
                return chosen[0];

            return new FidelityPositionsFile
            {
                FileDate = chosen[0].FileDate,
                AccountNumber = chosen[0].AccountNumber,
                AccountName = chosen[0].AccountName,
                Cash = chosen.Select(file => file.Cash).LastOrDefault(cash => cash is not null),
                CashSymbol = chosen.Select(file => file.CashSymbol).LastOrDefault(symbol => !string.IsNullOrWhiteSpace(symbol)) ?? "",
                CashDescription = chosen.Select(file => file.CashDescription).LastOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? "",
                Holdings = chosen.SelectMany(file => file.Holdings).ToList()
            };
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
