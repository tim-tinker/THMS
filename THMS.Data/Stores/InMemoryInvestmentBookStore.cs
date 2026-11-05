using THMS.Domain.Finance.Investments;

namespace THMS.Data.Stores
{
    public sealed class InMemoryInvestmentBookStore : IInvestmentBookStore
    {
        private readonly List<InvestmentAction> _actions = [];
        private readonly List<SecurityQuote> _quotes = [];
        private readonly List<SecurityMarketQuote> _marketQuotes = [];
        private readonly Dictionary<string, InvestmentSecurity> _securities = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<InvestmentAction> GetActions(Guid accountId) =>
            _actions
                .Where(action => action.AccountId == accountId)
                .OrderBy(action => action.Sequence)
                .ToList();

        public void UpsertAction(InvestmentAction action)
        {
            var index = _actions.FindIndex(existing =>
                existing.AccountId == action.AccountId && existing.ImportKey == action.ImportKey);
            if (index >= 0)
                _actions[index] = action;
            else
                _actions.Add(action);
        }

        public IReadOnlyList<SecurityQuote> GetQuotes(Guid accountId) =>
            _quotes.Where(quote => quote.AccountId == accountId).ToList();

        public void UpsertQuote(SecurityQuote quote)
        {
            var index = _quotes.FindIndex(existing =>
                existing.AccountId == quote.AccountId
                && string.Equals(existing.Symbol, quote.Symbol, StringComparison.OrdinalIgnoreCase)
                && existing.AsOf.Date == quote.AsOf.Date);
            if (index >= 0)
            {
                quote.Id = _quotes[index].Id;
                _quotes[index] = quote;
            }
            else
            {
                _quotes.Add(quote);
            }
        }

        public IReadOnlyList<SecurityMarketQuote> GetMarketQuotes() =>
            _marketQuotes.Select(quote => quote.Copy()).ToList();

        public void UpsertMarketQuote(SecurityMarketQuote quote)
        {
            var index = _marketQuotes.FindIndex(existing => SecurityMarketQuote.SameObservation(existing, quote));
            if (index >= 0)
                _marketQuotes[index] = quote.Copy();
            else
                _marketQuotes.Add(quote.Copy());
        }

        public IReadOnlyList<InvestmentSecurity> GetSecurities() => _securities.Values.ToList();

        public void UpsertSecurity(InvestmentSecurity security)
        {
            _securities[security.Symbol] = security;
        }

        public void DeleteAccount(Guid accountId)
        {
            _actions.RemoveAll(action => action.AccountId == accountId);
            _quotes.RemoveAll(quote => quote.AccountId == accountId);
        }
    }
}
