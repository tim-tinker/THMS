using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.Investments
{
    public sealed class InvestmentDashboardComposer
    {
        private readonly IAccountDataStore _accounts;
        private readonly IInvestmentBookStore _books;

        public InvestmentDashboardComposer()
            : this(new DataStoreFactory().GetAccountStore(), new DataStoreFactory().GetInvestmentBookStore())
        {
        }

        public InvestmentDashboardComposer(IAccountDataStore accounts, IInvestmentBookStore books)
        {
            _accounts = accounts;
            _books = books;
        }

        public InvestmentDashboardSnapshot Compose(DateTime asOf)
        {
            asOf = asOf.Date;
            var accounts = _accounts.GetAllAccounts().OfType<InvestmentAccount>().ToList();
            if (accounts.Count == 0)
            {
                return new InvestmentDashboardSnapshot
                {
                    Note = "No investment accounts."
                };
            }

            var combined = new SortedDictionary<DateTime, MonthTotal>();
            decimal estimated = 0;
            decimal untracked = 0;
            var anyHistory = false;

            foreach (var account in accounts)
            {
                var state = Load(account);
                var tracked = account.TracksPositions
                    || state.OpenedOn is not null
                    || state.Actions.Any(action => action.Status == InvestmentActionStatus.Applied);
                if (!tracked)
                {
                    untracked += account.CashBalance + account.PositionValue;
                    continue;
                }

                estimated += InvestmentBookkeeper.Replay(state).EstimatedAnnualIncome;
                var history = InvestmentMonthlySummary.Build(state, asOf);
                if (history.Count == 0)
                {
                    untracked += account.CashBalance + account.PositionValue;
                    continue;
                }

                anyHistory = true;
                foreach (var month in history)
                {
                    if (!combined.TryGetValue(month.Month, out var total))
                    {
                        total = new MonthTotal();
                        combined[month.Month] = total;
                    }

                    total.Value += month.Value;
                    total.Income += month.Income;
                    total.EstimatedAnnualIncome += month.EstimatedAnnualIncome;
                    total.Holdings += month.Holdings;
                    total.CarriedForward += month.CarriedForward;
                    total.Unpriced += month.Unpriced;
                }
            }

            var chronological = new List<InvestmentDashboardMonthRow>();
            decimal? previous = null;
            foreach (var (month, total) in combined)
            {
                var change = previous is decimal prior ? total.Value - prior : (decimal?)null;
                previous = total.Value;
                chronological.Add(new InvestmentDashboardMonthRow
                {
                    Month = month,
                    Label = month.ToString("MMM yyyy"),
                    Value = total.Value,
                    Change = change,
                    Income = total.Income,
                    EstimatedAnnualIncome = total.EstimatedAnnualIncome,
                    Prices = PriceText(total)
                });
            }

            var current = new DateTime(asOf.Year, asOf.Month, 1);
            var window = current.AddMonths(-11);
            var latest = chronological.LastOrDefault();
            var incomplete = latest is not null && combined.TryGetValue(latest.Month, out var latestTotal) && latestTotal.Unpriced > 0;
            var note = InvestmentDashboardSnapshot.MethodNote;
            if (!anyHistory)
                note = "No position history yet. " + note;
            else if (incomplete)
                note += " Holdings with no price are left out of the total.";
            if (untracked != 0)
                note += " The total also includes accounts that have no position history.";

            return new InvestmentDashboardSnapshot
            {
                TotalValue = (latest?.Value ?? 0) + untracked,
                IncomeThisMonth = chronological.Where(row => row.Month == current).Sum(row => row.Income),
                IncomeLastTwelveMonths = chronological.Where(row => row.Month >= window).Sum(row => row.Income),
                EstimatedAnnualIncome = estimated,
                PricesIncomplete = incomplete,
                Note = note,
                Months = chronological
            };
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

        private static string PriceText(MonthTotal month)
        {
            if (month.Holdings == 0)
                return "Cash";
            if (month.Unpriced > 0 && month.CarriedForward > 0)
                return $"{Count(month.Unpriced, "unpriced")}; others carried forward";
            if (month.Unpriced > 0)
                return Count(month.Unpriced, "unpriced");
            if (month.CarriedForward == month.Holdings)
                return "Carried forward";
            if (month.CarriedForward > 0)
                return "Some prices carried forward";
            return "Priced this month";
        }

        private static string Count(int count, string word) => count == 1 ? "1 " + word : $"{count} {word}";

        private sealed class MonthTotal
        {
            public decimal Value { get; set; }
            public decimal Income { get; set; }
            public decimal EstimatedAnnualIncome { get; set; }
            public int Holdings { get; set; }
            public int CarriedForward { get; set; }
            public int Unpriced { get; set; }
        }
    }
}
