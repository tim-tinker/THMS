using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Investments;
using THMS.Logic.Finance.Model;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.NetWorth
{
    public sealed class NetWorthComposer
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly IAccountStatementDataStore _statements;
        private readonly IInvestmentBookStore _books;
        private readonly IAssetDataStore _assets;

        public NetWorthComposer()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetAccountStatementStore(),
                new DataStoreFactory().GetInvestmentBookStore(),
                new DataStoreFactory().GetAssetStore())
        {
        }

        public NetWorthComposer(
            IAccountDataStore accounts,
            ITransactionDataStore transactions,
            IAccountStatementDataStore statements,
            IInvestmentBookStore books,
            IAssetDataStore assets)
        {
            _accounts = accounts;
            _transactions = transactions;
            _statements = statements;
            _books = books;
            _assets = assets;
        }

        public NetWorthSnapshot Compose(DateTime asOf)
        {
            asOf = asOf.Date;
            var current = new DateTime(asOf.Year, asOf.Month, 1);
            var accounts = _accounts.GetAllAccounts().Where(account => account is not InternalAccount).ToList();
            var assets = _assets.GetAll().ToList();
            var names = assets.ToDictionary(asset => asset.Id, asset => asset.Name);
            var valuations = assets.ToDictionary(asset => asset.Id, asset => _assets.GetValuations(asset.Id));

            var cashAccounts = accounts.OfType<BankAccount>().Cast<Account>().ToList();
            var debtAccounts = accounts.Where(account => account is CreditAccount or LoanAccount or MortgageAccount).ToList();
            var activity = new Dictionary<Guid, List<DatedAmount>>();
            var statements = new Dictionary<Guid, List<AccountStatement>>();
            foreach (var account in cashAccounts.Concat(debtAccounts))
            {
                statements[account.Id] = _statements.GetForAccount(account.Id);
                activity[account.Id] = LoadActivity(account.Id);
            }

            var investments = BuildInvestments(accounts.OfType<InvestmentAccount>(), asOf);
            var start = EarliestMonth(current, investments, assets, valuations, statements, cashAccounts.Concat(debtAccounts));
            var months = new List<NetWorthMonthRow>();
            decimal? previous = null;

            for (var month = start; month <= current; month = month.AddMonths(1))
            {
                var through = month == current ? asOf : month.AddMonths(1).AddDays(-1);
                var live = month == current;
                var cash = SumBalances(cashAccounts, statements, activity, through, live);
                var invested = investments.ValueOn(month) + (live ? investments.Untracked : 0m);
                var property = 0m;
                var vehicles = 0m;
                var personal = 0m;
                var linkedLeaseIds = LinkedLeaseIds(debtAccounts, assets);
                var debt = SumKeptDebt(debtAccounts, linkedLeaseIds, statements, activity, through, live);
                foreach (var asset in assets)
                {
                    var lease = LeaseOn(asset, debtAccounts);
                    var chosen = ValuationPolicy.Choose(asset, valuations[asset.Id], through);
                    if (lease is null)
                    {
                        if (chosen is not null)
                            AddOwned(asset, chosen.Amount, ref property, ref vehicles, ref personal);
                        continue;
                    }

                    var balance = LeaseBalance(lease, statements, activity, through, live);
                    if (balance is null)
                        continue;
                    if (chosen is null)
                    {
                        debt += CostToKeep(balance.Value, lease.Residual);
                        continue;
                    }

                    var equity = LeaseEquity.IfKept(chosen.Amount, balance.Value, lease.Residual);
                    if (equity >= 0)
                        AddOwned(asset, equity, ref property, ref vehicles, ref personal);
                    else
                        debt += equity;
                }

                var net = cash + invested + property + vehicles + personal + debt;
                months.Add(new NetWorthMonthRow
                {
                    Month = month,
                    Label = month.ToString("MMM yyyy"),
                    NetWorth = net,
                    Change = previous is decimal prior ? net - prior : null,
                    Cash = cash,
                    Investments = invested,
                    Property = property,
                    Vehicles = vehicles,
                    Personal = personal,
                    Debt = debt
                });
                previous = net;
            }

            var latest = months[^1];
            var lines = BuildLines(asOf, cashAccounts, debtAccounts, names, investments, assets, valuations);
            return new NetWorthSnapshot
            {
                NetWorth = latest.NetWorth,
                Cash = latest.Cash,
                Investments = latest.Investments,
                OtherAssets = latest.Property + latest.Vehicles + latest.Personal,
                Debt = latest.Debt,
                Note = Note(investments, lines),
                Months = months,
                Lines = lines
            };
        }

        private static string Note(InvestmentSeries investments, IReadOnlyList<NetWorthLine> lines)
        {
            var note = NetWorthSnapshot.MethodNote;
            if (investments.Untracked != 0m)
                note += " Investment accounts with no position history are included in the current month only.";
            if (investments.PricesIncomplete)
                note += " Holdings with no price are left out of the investment total.";
            var missing = lines.Count(line => line.Amount is null);
            if (missing == 1)
                note += " 1 asset has no valuation.";
            else if (missing > 1)
                note += $" {missing} assets have no valuation.";
            return note;
        }

        private List<NetWorthLine> BuildLines(
            DateTime asOf,
            IReadOnlyList<Account> cashAccounts,
            IReadOnlyList<Account> debtAccounts,
            IReadOnlyDictionary<Guid, string> assetNames,
            InvestmentSeries investments,
            IReadOnlyList<Asset> assets,
            IReadOnlyDictionary<Guid, IReadOnlyList<AssetValuation>> valuations)
        {
            var lines = new List<NetWorthLine>();
            foreach (var account in cashAccounts)
            {
                lines.Add(new NetWorthLine
                {
                    Group = "Cash",
                    Name = account.Name ?? "",
                    Amount = StoredBalance(account),
                    Source = "Current balance"
                });
            }

            foreach (var line in investments.Lines)
            {
                lines.Add(new NetWorthLine
                {
                    Group = "Investments",
                    Name = line.Name,
                    Amount = line.Amount,
                    Source = line.Tracked ? "Market value" : "No position history"
                });
            }

            var linked = debtAccounts.OfType<LeaseAccount>()
                .Where(lease => lease.SecuredAssetId is Guid id && assets.Any(asset => asset.Id == id))
                .ToDictionary(lease => lease.SecuredAssetId!.Value);

            foreach (var asset in assets)
            {
                var chosen = ValuationPolicy.Choose(asset, valuations[asset.Id], asOf);
                if (!linked.TryGetValue(asset.Id, out var lease))
                {
                    lines.Add(new NetWorthLine
                    {
                        Group = GroupOf(asset),
                        Name = asset.Name,
                        Amount = chosen?.Amount,
                        Source = chosen is null ? "No valuation" : chosen.Describe(asOf)
                    });
                    continue;
                }

                if (chosen is null)
                {
                    lines.Add(new NetWorthLine
                    {
                        Group = "Debt",
                        Name = asset.Name,
                        Amount = CostToKeep(lease.Principal, lease.Residual),
                        Source = "No valuation. Keeping it costs the remaining payments and the residual."
                    });
                    continue;
                }

                var equity = LeaseEquity.IfKept(chosen.Amount, lease.Principal, lease.Residual);
                lines.Add(new NetWorthLine
                {
                    Group = equity >= 0 ? GroupOf(asset) : "Debt",
                    Name = asset.Name,
                    Amount = equity,
                    Source = KeptSource(chosen, lease, asOf)
                });
            }

            foreach (var account in debtAccounts)
            {
                if (account is LeaseAccount linkedLease && linked.ContainsKey(linkedLease.SecuredAssetId ?? Guid.Empty))
                    continue;

                var amount = account is LeaseAccount lease
                    ? CostToKeep(lease.Principal, lease.Residual)
                    : StoredBalance(account);
                lines.Add(new NetWorthLine
                {
                    Group = "Debt",
                    Name = account.Name ?? "",
                    Amount = amount,
                    Source = account is LeaseAccount
                        ? "Remaining payments and residual"
                        : DebtSource(account, assetNames)
                });
            }

            return lines
                .OrderBy(line => GroupOrder(line.Group))
                .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string KeptSource(ChosenValuation valuation, LeaseAccount lease, DateTime asOf)
        {
            var owed = lease.Principal < 0 ? -lease.Principal : lease.Principal;
            return $"{valuation.Describe(asOf)}. Keeping it is that value, minus {owed:c2} still due, minus {lease.Residual:c2} residual.";
        }

        private static HashSet<Guid> LinkedLeaseIds(IReadOnlyList<Account> debtAccounts, IReadOnlyList<Asset> assets)
        {
            var assetIds = assets.Select(asset => asset.Id).ToHashSet();
            return debtAccounts.OfType<LeaseAccount>()
                .Where(lease => lease.SecuredAssetId is Guid id && assetIds.Contains(id))
                .Select(lease => lease.Id)
                .ToHashSet();
        }

        private static LeaseAccount? LeaseOn(Asset asset, IReadOnlyList<Account> debtAccounts) =>
            debtAccounts.OfType<LeaseAccount>().FirstOrDefault(lease => lease.SecuredAssetId == asset.Id);

        private static decimal? LeaseBalance(
            LeaseAccount lease,
            IReadOnlyDictionary<Guid, List<AccountStatement>> statements,
            IReadOnlyDictionary<Guid, List<DatedAmount>> activity,
            DateTime through,
            bool live) =>
            live
                ? lease.Principal
                : BalanceAsOf(lease, statements[lease.Id], activity[lease.Id], through);

        private static decimal CostToKeep(decimal leaseBalance, decimal residual) =>
            leaseBalance - residual;

        private decimal SumKeptDebt(
            IReadOnlyList<Account> debtAccounts,
            HashSet<Guid> linkedLeaseIds,
            IReadOnlyDictionary<Guid, List<AccountStatement>> statements,
            IReadOnlyDictionary<Guid, List<DatedAmount>> activity,
            DateTime through,
            bool live)
        {
            decimal total = 0;
            foreach (var account in debtAccounts)
            {
                if (linkedLeaseIds.Contains(account.Id))
                    continue;
                if (live)
                {
                    total += account is LeaseAccount lease
                        ? CostToKeep(lease.Principal, lease.Residual)
                        : StoredBalance(account);
                    continue;
                }

                if (BalanceAsOf(account, statements[account.Id], activity[account.Id], through) is not decimal balance)
                    continue;
                total += account is LeaseAccount openLease
                    ? CostToKeep(balance, openLease.Residual)
                    : balance;
            }

            return total;
        }

        private static void AddOwned(
            Asset asset,
            decimal amount,
            ref decimal property,
            ref decimal vehicles,
            ref decimal personal)
        {
            switch (AssetKinds.Of(asset))
            {
                case AssetKinds.Property:
                    property += amount;
                    break;
                case AssetKinds.Vehicle:
                    vehicles += amount;
                    break;
                default:
                    personal += amount;
                    break;
            }
        }

        private static string DebtSource(Account account, IReadOnlyDictionary<Guid, string> assetNames)
        {
            Guid? secured = account switch
            {
                LoanAccount loan => loan.SecuredAssetId,
                MortgageAccount mortgage => mortgage.SecuredAssetId,
                _ => null
            };
            return secured is Guid assetId && assetNames.TryGetValue(assetId, out var name) && name.Length > 0
                ? $"Current balance, secured by {name}"
                : "Current balance";
        }

        private static string GroupOf(Asset asset) => AssetKinds.Of(asset) switch
        {
            AssetKinds.Property => "Property",
            AssetKinds.Vehicle => "Vehicles",
            _ => "Personal"
        };

        private static int GroupOrder(string group) => group switch
        {
            "Cash" => 0,
            "Investments" => 1,
            "Property" => 2,
            "Vehicles" => 3,
            "Personal" => 4,
            "Debt" => 5,
            _ => 6
        };

        private decimal SumBalances(
            IEnumerable<Account> accounts,
            IReadOnlyDictionary<Guid, List<AccountStatement>> statements,
            IReadOnlyDictionary<Guid, List<DatedAmount>> activity,
            DateTime through,
            bool live)
        {
            decimal total = 0;
            foreach (var account in accounts)
            {
                if (live)
                {
                    total += StoredBalance(account);
                    continue;
                }

                if (BalanceAsOf(account, statements[account.Id], activity[account.Id], through) is decimal balance)
                    total += balance;
            }

            return total;
        }

        private static decimal? BalanceAsOf(
            Account account,
            IEnumerable<AccountStatement> statements,
            List<DatedAmount> activity,
            DateTime through)
        {
            if (!PostedBalanceCalculator.TryResolveLatestStatement(
                    account,
                    statements.Where(statement => statement.StatementDate.Date <= through.Date),
                    out _,
                    out var anchor))
                return null;

            return anchor.LedgerBalance + ActivityBetween(activity, anchor.AsOf, through);
        }

        private static decimal ActivityBetween(List<DatedAmount> activity, DateTime after, DateTime through)
        {
            decimal sum = 0;
            foreach (var row in activity)
            {
                if (row.Date > after.Date && row.Date <= through.Date)
                    sum += row.Amount;
            }

            return sum;
        }

        private List<DatedAmount> LoadActivity(Guid accountId)
        {
            var rows = new List<DatedAmount>();
            foreach (var transaction in _transactions.GetPostedTransactions(accountId))
                rows.Add(new DatedAmount(transaction.Date.Date, transaction.Amount));
            foreach (var transfer in _transactions.GetPostedTransferTransactions(accountId))
                rows.Add(new DatedAmount(transfer.Date.Date, transfer.Amount));
            return rows;
        }

        private static decimal StoredBalance(Account account) => account switch
        {
            BankAccount bank => bank.PostedBalance,
            CreditAccount credit => credit.PostedBalance,
            LoanAccount loan => loan.Principal,
            MortgageAccount mortgage => mortgage.Principal,
            _ => 0m
        };

        private InvestmentSeries BuildInvestments(IEnumerable<InvestmentAccount> accounts, DateTime asOf)
        {
            var series = new InvestmentSeries();
            foreach (var account in accounts)
            {
                var state = LoadBook(account);
                var tracked = account.TracksPositions
                    || state.OpenedOn is not null
                    || state.Actions.Any(action => action.Status == InvestmentActionStatus.Applied);
                var history = tracked ? InvestmentMonthlySummary.Build(state, asOf) : [];
                if (history.Count == 0)
                {
                    var stored = account.CashBalance + account.PositionValue;
                    series.Untracked += stored;
                    series.Lines.Add(new InvestmentLine(account.Name ?? "", stored, false));
                    continue;
                }

                foreach (var month in history)
                    series.Add(month.Month, month.Value);
                if (history[^1].Unpriced > 0)
                    series.PricesIncomplete = true;
                series.Lines.Add(new InvestmentLine(account.Name ?? "", history[^1].Value, true));
            }

            return series;
        }

        private InvestmentBookState LoadBook(InvestmentAccount account)
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

        private static DateTime EarliestMonth(
            DateTime current,
            InvestmentSeries investments,
            IReadOnlyList<Asset> assets,
            IReadOnlyDictionary<Guid, IReadOnlyList<AssetValuation>> valuations,
            IReadOnlyDictionary<Guid, List<AccountStatement>> statements,
            IEnumerable<Account> balanceAccounts)
        {
            var start = current;
            foreach (var month in investments.Months)
            {
                if (month < start)
                    start = month;
            }

            foreach (var asset in assets)
            {
                if (!valuations.TryGetValue(asset.Id, out var values))
                    continue;
                foreach (var valuation in values)
                {
                    if (!ValuationPolicy.Counts(asset, valuation.ValueType))
                        continue;
                    var month = new DateTime(valuation.AsOf.Year, valuation.AsOf.Month, 1);
                    if (month < start)
                        start = month;
                }
            }

            foreach (var account in balanceAccounts)
            {
                if (!statements.TryGetValue(account.Id, out var accountStatements))
                    continue;
                foreach (var statement in accountStatements)
                {
                    if (!PostedBalanceCalculator.TryGetStatementAnchor(account, statement, out var anchor))
                        continue;
                    var month = new DateTime(anchor.AsOf.Year, anchor.AsOf.Month, 1);
                    if (month < start)
                        start = month;
                }
            }

            return start;
        }

        private readonly record struct DatedAmount(DateTime Date, decimal Amount);

        private sealed class InvestmentSeries
        {
            private readonly Dictionary<DateTime, decimal> _months = [];

            public decimal Untracked { get; set; }
            public bool PricesIncomplete { get; set; }
            public List<InvestmentLine> Lines { get; } = [];
            public IEnumerable<DateTime> Months => _months.Keys;

            public void Add(DateTime month, decimal value) =>
                _months[month] = _months.GetValueOrDefault(month) + value;

            public decimal ValueOn(DateTime month) => _months.GetValueOrDefault(month);
        }

        private readonly record struct InvestmentLine(string Name, decimal Amount, bool Tracked);
    }
}
