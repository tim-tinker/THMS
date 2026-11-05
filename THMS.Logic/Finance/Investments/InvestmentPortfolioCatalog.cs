using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;

namespace THMS.Logic.Finance.Investments
{
    public sealed class InvestmentPortfolioRow
    {
        public Guid AccountId { get; init; }
        public string Name { get; init; } = "";
        public string Institution { get; init; } = "";
        public string WebsiteUrl { get; init; } = "";
        public string AccountNumber { get; init; } = "";
        public decimal Cash { get; init; }
        public decimal? MarketValue { get; init; }
        public decimal? AccountValue { get; init; }
        public DateTime? PricesAsOf { get; init; }
        public bool PricesIncomplete { get; init; }
        public string PricesAsOfText
        {
            get
            {
                if (PricesAsOf is not DateTime asOf)
                    return PricesIncomplete ? "No price" : "";
                var date = asOf.ToShortDateString();
                return PricesIncomplete ? date + " (incomplete)" : date;
            }
        }
        public QuoteAccess QuoteAccess { get; init; }
        public string PriceSource => QuoteAccess == QuoteAccess.Service ? "Queryable" : "Import only";
        public decimal EstimatedAnnualIncome { get; init; }
    }

    public sealed class InvestmentPortfolioCatalog
    {
        private readonly IAccountDataStore _accounts;
        private readonly IInvestmentBookStore _books;

        public InvestmentPortfolioCatalog()
            : this(new DataStoreFactory().GetAccountStore(), new DataStoreFactory().GetInvestmentBookStore())
        {
        }

        public InvestmentPortfolioCatalog(IAccountDataStore accounts, IInvestmentBookStore books)
        {
            _accounts = accounts;
            _books = books;
        }

        public IReadOnlyList<InvestmentPortfolioRow> Portfolios()
        {
            var rows = new List<InvestmentPortfolioRow>();
            foreach (var account in _accounts.GetAllAccounts().OfType<InvestmentAccount>().OrderBy(account => account.Name))
            {
                var state = Load(account);
                var snapshot = InvestmentBookkeeper.Replay(state);
                var tracked = account.TracksPositions
                    || state.OpenedOn is not null
                    || state.Actions.Any(action => action.Status == InvestmentActionStatus.Applied);
                rows.Add(new InvestmentPortfolioRow
                {
                    AccountId = account.Id,
                    Name = account.Name ?? "",
                    Institution = account.Institution ?? "",
                    WebsiteUrl = account.WebsiteUrl ?? "",
                    AccountNumber = account.AccountNumber ?? "",
                    Cash = tracked ? snapshot.Cash : account.CashBalance,
                    MarketValue = tracked && snapshot.PricesIncomplete ? null : tracked ? snapshot.PositionValue : account.PositionValue,
                    AccountValue = tracked && snapshot.PricesIncomplete ? null : tracked ? snapshot.AccountValue : account.AccountValue,
                    PricesAsOf = tracked ? snapshot.PricesAsOf : account.BalanceAsOf,
                    PricesIncomplete = tracked && snapshot.PricesIncomplete,
                    QuoteAccess = account.QuoteAccess,
                    EstimatedAnnualIncome = tracked ? snapshot.EstimatedAnnualIncome : 0
                });
            }

            return rows;
        }

        public IReadOnlyList<InvestmentPositionRow> Securities(Guid accountId)
        {
            var account = _accounts.GetAllAccounts().OfType<InvestmentAccount>().FirstOrDefault(item => item.Id == accountId);
            if (account is null)
                return [];

            var state = Load(account);
            var open = InvestmentBookkeeper.Replay(state).Positions.ToList();
            var seen = new HashSet<string>(
                open.Select(row => InvestmentSymbols.Normalize(row.Symbol)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var action in state.Actions)
            {
                var symbol = InvestmentSymbols.Normalize(action.Symbol);
                if (symbol.Length == 0 || !seen.Add(symbol))
                    continue;

                var security = state.Securities.FirstOrDefault(item =>
                    string.Equals(InvestmentSymbols.Normalize(item.Symbol), symbol, StringComparison.OrdinalIgnoreCase));
                if (security?.IsSweep == true || InvestmentSymbols.IsSweep(symbol, action.Description) || InvestmentSymbols.IsSweep(symbol, security?.Name))
                {
                    seen.Remove(symbol);
                    continue;
                }

                var quote = state.Quotes
                    .Where(item => string.Equals(InvestmentSymbols.Normalize(item.Symbol), symbol, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.AsOf)
                    .FirstOrDefault();
                var valuation = InvestmentBookkeeper.Valuation(state, symbol);
                open.Add(new InvestmentPositionRow
                {
                    Symbol = symbol,
                    Name = security?.Name ?? "",
                    Shares = 0,
                    Price = valuation.Price,
                    MarketValue = valuation.Price is null ? null : 0,
                    PriceAsOf = valuation.AsOf,
                    EstimatedAnnualIncome = 0,
                    AmountPerShare = quote?.AmountPerShare,
                    ExDate = quote?.ExDate,
                    PayDate = quote?.PayDate
                });
            }

            return open.OrderBy(row => row.Symbol, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public IReadOnlyList<InvestmentAction> Actions(Guid accountId, string symbol)
        {
            var wanted = InvestmentSymbols.Normalize(symbol);
            if (wanted.Length == 0)
                return [];

            return _books.GetActions(accountId)
                .Where(action => string.Equals(InvestmentSymbols.Normalize(action.Symbol), wanted, StringComparison.OrdinalIgnoreCase))
                .OrderBy(action => action.RunDate)
                .ThenBy(action => action.Sequence)
                .ToList();
        }

        private InvestmentBookState Load(InvestmentAccount account)
        {
            var state = new InvestmentBookState
            {
                AccountId = account.Id,
                OpenedOn = account.BookOpenedOn
            };
            state.Actions.AddRange(_books.GetActions(account.Id));
            state.Quotes.AddRange(_books.GetQuotes(account.Id));
            state.MarketQuotes.AddRange(_books.GetMarketQuotes());
            state.Securities.AddRange(_books.GetSecurities());
            return state;
        }
    }
}
