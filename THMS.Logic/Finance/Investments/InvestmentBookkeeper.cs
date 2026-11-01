using System.Globalization;
using System.Text.RegularExpressions;
using THMS.Domain.Finance.Investments;

namespace THMS.Logic.Finance.Investments
{
    public static class InvestmentBookkeeper
    {
        public static InvestmentImportPreview Import(
            InvestmentBookState state,
            FidelityPositionsFile? positions,
            FidelityHistoryFile? history)
        {
            var messages = new List<string>();
            var changes = new List<InvestmentAction>();
            var applied = 0;
            var skipped = 0;
            var updated = 0;
            var review = 0;

            var openedThisCall = false;
            if (state.OpenedOn is null && positions is not null)
            {
                ApplyPositions(state, positions, messages, changes, ref applied, ref skipped);
                openedThisCall = state.OpenedOn is not null;
            }

            if (history is not null)
                ApplyHistory(state, history, messages, changes, ref applied, ref skipped, ref updated, ref review);

            if (positions is not null && state.OpenedOn is not null && !openedThisCall)
                ApplyPositions(state, positions, messages, changes, ref applied, ref skipped);

            return new InvestmentImportPreview
            {
                Applied = applied,
                Skipped = skipped,
                Updated = updated,
                Review = review,
                Messages = messages,
                Changes = changes,
                Snapshot = Replay(state)
            };
        }

        public static string? Classify(InvestmentBookState state, Guid actionId, InvestmentActionKind kind)
        {
            var action = state.Actions.FirstOrDefault(candidate => candidate.Id == actionId);
            if (action is null)
                return "That row is not in the book.";
            if (action.Status != InvestmentActionStatus.Review)
                return "That row is already classified.";
            if (kind is InvestmentActionKind.Unknown or InvestmentActionKind.Opening or InvestmentActionKind.OpeningCash)
                return "Choose a kind that can be applied.";

            action.Kind = kind;
            if (kind == InvestmentActionKind.Split)
                action.SplitFactor = SplitFactor(action.ActionText, action.Description);
            PrepareEffect(action);
            action.Status = InvestmentActionStatus.Applied;
            var snapshot = Replay(state);
            if (snapshot.Failed.Contains(action.Id))
            {
                action.Status = InvestmentActionStatus.Review;
                action.Note = FailureNote(action);
                return action.Note;
            }

            action.Note = null;
            action.BookCashAfter = snapshot.CashAfter.TryGetValue(action.Id, out var cash) ? cash : null;
            if (kind == InvestmentActionKind.Split && action.SplitFactor is decimal factor)
                AddSplitQuote(state, action.Symbol, action.RunDate, factor);
            return null;
        }

        public static InvestmentSnapshot Replay(InvestmentBookState state)
        {
            var lots = new List<Lot>();
            var cashAfter = new Dictionary<Guid, decimal>();
            var failed = new HashSet<Guid>();
            decimal cash = 0;
            var applied = state.Actions
                .Where(action => action.Status == InvestmentActionStatus.Applied)
                .OrderBy(action => action.RunDate)
                .ThenBy(action => action.Sequence)
                .ToList();

            var markers = state.Quotes
                .Select(quote => quote.AsOf.Date)
                .Concat(applied.Select(action => action.RunDate.Date))
                .Distinct()
                .OrderBy(date => date)
                .ToList();
            var income = new List<InvestmentIncomePoint>();
            var markerIndex = 0;

            foreach (var action in applied)
            {
                while (markerIndex < markers.Count && markers[markerIndex] < action.RunDate.Date)
                {
                    RecordIncome(income, markers[markerIndex], lots, state);
                    markerIndex++;
                }

                if (!Apply(action, lots, ref cash))
                    failed.Add(action.Id);
                cashAfter[action.Id] = cash;

                var record = markerIndex < markers.Count && markers[markerIndex] == action.RunDate.Date;
                var lastOfDay = action == applied.Last(candidate => candidate.RunDate.Date == action.RunDate.Date);
                if (record && lastOfDay)
                {
                    RecordIncome(income, action.RunDate.Date, lots, state);
                    markerIndex++;
                }
            }

            while (markerIndex < markers.Count)
            {
                RecordIncome(income, markers[markerIndex], lots, state);
                markerIndex++;
            }

            var positions = Positions(lots, state);
            var positionValue = positions.Sum(row => row.MarketValue ?? 0);
            var annual = Income(lots, state, DateTime.MaxValue.Date);
            return new InvestmentSnapshot
            {
                Cash = cash,
                PositionValue = positionValue,
                AccountValue = cash + positionValue,
                EstimatedAnnualIncome = annual,
                Positions = positions,
                Income = income,
                CashAfter = cashAfter,
                Failed = failed
            };
        }

        private static void ApplyPositions(
            InvestmentBookState state,
            FidelityPositionsFile file,
            List<string> messages,
            List<InvestmentAction> changes,
            ref int applied,
            ref int skipped)
        {
            if (!string.IsNullOrWhiteSpace(file.CashSymbol))
            {
                Remember(state, file.CashSymbol, file.CashDescription, isSweep: true);
            }

            foreach (var holding in file.Holdings)
                Remember(state, holding.Symbol, holding.Description, isSweep: false);

            if (state.OpenedOn is null)
            {
                if (file.Cash is null && file.Holdings.Count == 0)
                {
                    messages.Add("The positions file has no cash and no holdings.");
                    return;
                }

                state.OpenedOn = file.FileDate.Date;
                if (file.Cash is decimal cash)
                {
                    var openingCash = Add(state, new InvestmentAction
                    {
                        AccountId = state.AccountId,
                        RunDate = file.FileDate.Date,
                        Kind = InvestmentActionKind.OpeningCash,
                        Status = InvestmentActionStatus.Applied,
                        Amount = cash,
                        CashChange = cash,
                        ActionText = "Opening cash",
                        Symbol = file.CashSymbol,
                        Description = file.CashDescription,
                        ImportKey = $"opening-cash|{file.FileDate:yyyy-MM-dd}"
                    });
                    changes.Add(openingCash);
                    applied++;
                }
                else
                {
                    messages.Add("The positions file has no money-market cash row. Opening cash is zero.");
                }

                foreach (var holding in file.Holdings)
                {
                    var cost = CostPerShare(holding);
                    var action = Add(state, new InvestmentAction
                    {
                        AccountId = state.AccountId,
                        RunDate = file.FileDate.Date,
                        Kind = InvestmentActionKind.Opening,
                        Status = InvestmentActionStatus.Applied,
                        Symbol = holding.Symbol,
                        Description = holding.Description,
                        Quantity = holding.Quantity,
                        Price = cost,
                        Amount = holding.CostBasisTotal,
                        ShareChange = holding.Quantity,
                        ActionText = "Opening position",
                        ImportKey = $"opening|{holding.Symbol}|{file.FileDate:yyyy-MM-dd}"
                    });
                    changes.Add(action);
                    applied++;
                    UpsertQuote(state, holding, file.FileDate);
                }

                messages.Add($"Opened the book as of {file.FileDate:d} with {file.Holdings.Count} holdings.");
                return;
            }

            foreach (var holding in file.Holdings)
                UpsertQuote(state, holding, file.FileDate);

            var snapshot = Replay(state);
            var onBooks = snapshot.Positions.ToDictionary(row => row.Symbol, StringComparer.OrdinalIgnoreCase);
            var inFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var holding in file.Holdings)
            {
                inFile.Add(holding.Symbol);
                var shares = holding.Quantity ?? 0;
                if (!onBooks.TryGetValue(holding.Symbol, out var position))
                {
                    messages.Add($"{holding.Symbol} has {shares} shares in the positions file and none on the books.");
                    continue;
                }

                if (!SharesEqual(position.Shares, shares))
                    messages.Add($"{holding.Symbol} has {shares} shares in the positions file and {position.Shares} on the books.");
            }

            foreach (var position in snapshot.Positions)
            {
                if (!inFile.Contains(position.Symbol))
                    messages.Add($"{position.Symbol} has {position.Shares} shares on the books and is missing from the positions file.");
            }

            if (file.Cash is decimal fileCash && !MoneyEqual(fileCash, snapshot.Cash))
                messages.Add($"Money-market cash in the file is {fileCash:c} and the book has {snapshot.Cash:c}.");

            skipped++;
            messages.Add($"Updated prices and income rates as of {file.FileDate:d}. Share quantities were not changed.");
        }

        private static void ApplyHistory(
            InvestmentBookState state,
            FidelityHistoryFile file,
            List<string> messages,
            List<InvestmentAction> changes,
            ref int applied,
            ref int skipped,
            ref int updated,
            ref int review)
        {
            if (state.OpenedOn is null)
            {
                messages.Add("Import the positions view before the history file.");
                return;
            }

            foreach (var row in file.Rows)
            {
                var key = ImportKey(row);
                var existing = state.Actions.FirstOrDefault(action => action.ImportKey == key);
                if (existing is not null)
                {
                    if (IsProcessing(existing.CashBalanceText) && row.CashBalance is decimal settled)
                    {
                        existing.CashBalanceText = row.CashBalanceText;
                        existing.FileCashBalance = settled;
                        if (existing.BookCashAfter is decimal book && !MoneyEqual(book, settled))
                        {
                            existing.Note = $"Cash in the file is {settled:c} and the book had {book:c} after this row.";
                            messages.Add($"{row.RunDate:d} {row.Action}: {existing.Note}");
                        }

                        changes.Add(existing);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }

                    continue;
                }

                var action = NewHistoryAction(state, row, key);
                if (row.RunDate.Date <= state.OpenedOn.Value.Date)
                {
                    action.Status = InvestmentActionStatus.Skipped;
                    action.CashChange = 0;
                    action.ShareChange = 0;
                    action.Note = "Already included in the opening positions.";
                    Add(state, action);
                    changes.Add(action);
                    skipped++;
                    continue;
                }

                if (action.Status == InvestmentActionStatus.Review)
                {
                    Add(state, action);
                    changes.Add(action);
                    review++;
                    continue;
                }

                if (action.Status == InvestmentActionStatus.Skipped)
                {
                    Add(state, action);
                    changes.Add(action);
                    skipped++;
                    continue;
                }

                Add(state, action);
                var snapshot = Replay(state);
                if (snapshot.Failed.Contains(action.Id))
                {
                    action.Status = InvestmentActionStatus.Review;
                    action.Note = FailureNote(action);
                    action.CashChange = row.Amount;
                    action.ShareChange = row.Quantity;
                    changes.Add(action);
                    review++;
                    continue;
                }

                if (snapshot.CashAfter.TryGetValue(action.Id, out var bookCash))
                    action.BookCashAfter = bookCash;
                if (row.CashBalance is decimal fileCash && action.BookCashAfter is decimal after && !MoneyEqual(fileCash, after))
                {
                    action.Note = $"Cash in the file is {fileCash:c} and the book has {after:c}.";
                    messages.Add($"{row.RunDate:d} {row.Symbol} {row.Action}: {action.Note}");
                }

                if (action.Kind == InvestmentActionKind.Split && action.SplitFactor is decimal factor)
                    AddSplitQuote(state, action.Symbol, action.RunDate, factor);

                changes.Add(action);
                applied++;
            }
        }

        private static InvestmentAction NewHistoryAction(InvestmentBookState state, FidelityHistoryRow row, string key)
        {
            var sweep = InvestmentSymbols.IsSweep(row.Symbol, row.Description)
                || state.Securities.Any(security =>
                    security.IsSweep && string.Equals(security.Symbol, row.Symbol, StringComparison.OrdinalIgnoreCase));
            var kind = ClassifyKind(row.Action, row.Symbol);
            var action = new InvestmentAction
            {
                AccountId = state.AccountId,
                RunDate = row.RunDate.Date,
                ActionText = row.Action,
                Symbol = row.Symbol,
                Description = row.Description,
                Amount = row.Amount,
                Quantity = row.Quantity,
                Price = row.Price,
                Kind = kind,
                ImportKey = key,
                CashBalanceText = row.CashBalanceText,
                FileCashBalance = row.CashBalance
            };

            if (kind == InvestmentActionKind.Reinvestment && sweep)
            {
                action.Status = InvestmentActionStatus.Skipped;
                action.CashChange = 0;
                action.ShareChange = 0;
                action.Note = "Sweep reinvestment stays in cash.";
                return action;
            }

            if (kind == InvestmentActionKind.Reinvestment)
                action.Kind = InvestmentActionKind.Buy;

            if (action.Kind == InvestmentActionKind.Split)
                action.SplitFactor = SplitFactor(row.Action, row.Description);

            if (action.Kind == InvestmentActionKind.Unknown || !CanApply(action))
            {
                action.Status = InvestmentActionStatus.Review;
                action.Kind = action.Kind == InvestmentActionKind.Unknown ? InvestmentActionKind.Unknown : action.Kind;
                action.CashChange = row.Amount;
                action.ShareChange = row.Quantity;
                action.Note = action.Kind == InvestmentActionKind.Split
                    ? "The split ratio is missing, so the row was not applied."
                    : "Needs a classification before it is applied.";
                return action;
            }

            action.Status = InvestmentActionStatus.Applied;
            PrepareEffect(action);
            return action;
        }

        private static bool CanApply(InvestmentAction action)
        {
            switch (action.Kind)
            {
                case InvestmentActionKind.Buy:
                case InvestmentActionKind.Sell:
                    return action.Quantity is decimal quantity && quantity != 0 && action.Amount is not null;
                case InvestmentActionKind.Dividend:
                case InvestmentActionKind.Interest:
                case InvestmentActionKind.Income:
                case InvestmentActionKind.Contribution:
                case InvestmentActionKind.Distribution:
                    return action.Amount is not null;
                case InvestmentActionKind.Split:
                    return action.SplitFactor is > 0 && !string.IsNullOrWhiteSpace(action.Symbol);
                default:
                    return false;
            }
        }

        private static void PrepareEffect(InvestmentAction action)
        {
            switch (action.Kind)
            {
                case InvestmentActionKind.Buy:
                    var bought = Math.Abs(action.Quantity ?? 0);
                    action.ShareChange = bought;
                    action.CashChange = action.Amount;
                    if (action.Price is null && bought > 0 && action.Amount is decimal cost)
                        action.Price = Math.Abs(cost) / bought;
                    break;
                case InvestmentActionKind.Sell:
                    action.ShareChange = -Math.Abs(action.Quantity ?? 0);
                    action.CashChange = action.Amount;
                    break;
                case InvestmentActionKind.Split:
                    action.CashChange = 0;
                    break;
                default:
                    action.CashChange = action.Amount;
                    action.ShareChange = 0;
                    break;
            }
        }

        private static bool Apply(InvestmentAction action, List<Lot> lots, ref decimal cash)
        {
            switch (action.Kind)
            {
                case InvestmentActionKind.OpeningCash:
                    cash = action.Amount ?? 0;
                    return true;
                case InvestmentActionKind.Opening:
                    lots.Add(new Lot(action.Symbol, action.RunDate, action.Quantity ?? 0, action.Price ?? 0, action.Sequence));
                    return true;
                case InvestmentActionKind.Dividend:
                case InvestmentActionKind.Interest:
                case InvestmentActionKind.Income:
                case InvestmentActionKind.Contribution:
                case InvestmentActionKind.Distribution:
                    cash += action.Amount ?? 0;
                    return true;
                case InvestmentActionKind.Buy:
                    var shares = Math.Abs(action.Quantity ?? 0);
                    if (shares == 0)
                        return false;
                    cash += action.Amount ?? 0;
                    var cost = action.Price ?? (action.Amount is decimal amount && shares > 0 ? Math.Abs(amount) / shares : 0);
                    lots.Add(new Lot(action.Symbol, action.RunDate, shares, cost, action.Sequence));
                    return true;
                case InvestmentActionKind.Sell:
                    return Sell(action, lots, ref cash);
                case InvestmentActionKind.Split:
                    return Split(action, lots);
                default:
                    return false;
            }
        }

        private static bool Sell(InvestmentAction action, List<Lot> lots, ref decimal cash)
        {
            var remaining = Math.Abs(action.Quantity ?? 0);
            var available = lots.Where(lot => SameSymbol(lot.Symbol, action.Symbol)).Sum(lot => lot.Shares);
            if (remaining <= 0 || available + 0.0000001m < remaining)
                return false;

            cash += action.Amount ?? 0;
            foreach (var lot in lots
                .Where(lot => SameSymbol(lot.Symbol, action.Symbol) && lot.Shares > 0)
                .OrderBy(lot => lot.Acquired)
                .ThenBy(lot => lot.Sequence))
            {
                if (remaining <= 0)
                    break;
                var used = Math.Min(lot.Shares, remaining);
                lot.Shares -= used;
                remaining -= used;
            }

            return true;
        }

        private static bool Split(InvestmentAction action, List<Lot> lots)
        {
            if (action.SplitFactor is not decimal factor || factor <= 0)
                return false;
            var matched = false;
            foreach (var lot in lots.Where(lot => SameSymbol(lot.Symbol, action.Symbol) && lot.Shares > 0))
            {
                lot.Shares *= factor;
                if (lot.CostPerShare != 0)
                    lot.CostPerShare /= factor;
                matched = true;
            }

            return matched;
        }

        private static void UpsertQuote(InvestmentBookState state, FidelityPositionRow holding, DateTime fileDate)
        {
            var asOf = (holding.RateAsOf ?? fileDate).Date;
            var quote = state.Quotes.FirstOrDefault(existing =>
                SameSymbol(existing.Symbol, holding.Symbol) && existing.AsOf.Date == asOf);
            if (quote is null)
            {
                quote = new SecurityQuote
                {
                    AccountId = state.AccountId,
                    Symbol = holding.Symbol,
                    AsOf = asOf
                };
                state.Quotes.Add(quote);
            }

            quote.Price = holding.LastPrice;
            quote.DistributionRate = holding.DistributionRate;
            quote.AmountPerShare = holding.AmountPerShare;
            quote.ExDate = holding.ExDate;
            quote.PayDate = holding.PayDate;
            if (holding.EstimatedAnnualIncome is decimal income && holding.Quantity is decimal quantity && quantity != 0)
                quote.AnnualIncomePerShare = income / quantity;
        }

        private static void AddSplitQuote(InvestmentBookState state, string symbol, DateTime date, decimal factor)
        {
            if (factor <= 0 || string.IsNullOrWhiteSpace(symbol))
                return;
            if (state.Quotes.Any(quote => SameSymbol(quote.Symbol, symbol) && quote.AsOf.Date == date.Date))
                return;

            var prior = state.Quotes
                .Where(quote => SameSymbol(quote.Symbol, symbol) && quote.AsOf.Date < date.Date)
                .OrderByDescending(quote => quote.AsOf)
                .FirstOrDefault();
            if (prior is null)
                return;

            state.Quotes.Add(new SecurityQuote
            {
                AccountId = state.AccountId,
                Symbol = symbol,
                AsOf = date.Date,
                Price = prior.Price / factor,
                AnnualIncomePerShare = prior.AnnualIncomePerShare / factor,
                AmountPerShare = prior.AmountPerShare / factor,
                DistributionRate = prior.DistributionRate,
                ExDate = prior.ExDate,
                PayDate = prior.PayDate
            });
        }

        private static List<InvestmentPositionRow> Positions(List<Lot> lots, InvestmentBookState state)
        {
            var rows = new List<InvestmentPositionRow>();
            foreach (var group in lots.Where(lot => lot.Shares > 0.0000001m).GroupBy(lot => lot.Symbol, StringComparer.OrdinalIgnoreCase))
            {
                var shares = group.Sum(lot => lot.Shares);
                var basis = group.Sum(lot => lot.Shares * lot.CostPerShare);
                var quote = LatestQuote(state, group.Key, DateTime.MaxValue.Date);
                var name = state.Securities.FirstOrDefault(security => SameSymbol(security.Symbol, group.Key))?.Name ?? "";
                decimal? price = quote?.Price;
                decimal? income = quote?.AnnualIncomePerShare is decimal rate ? shares * rate : null;
                rows.Add(new InvestmentPositionRow
                {
                    Symbol = group.Key,
                    Name = name,
                    Shares = shares,
                    AverageCost = shares == 0 ? 0 : basis / shares,
                    TotalBasis = basis,
                    Price = price,
                    MarketValue = price is decimal last ? shares * last : null,
                    EstimatedAnnualIncome = income,
                    ExDate = quote?.ExDate,
                    PayDate = quote?.PayDate
                });
            }

            return rows.OrderBy(row => row.Symbol, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void RecordIncome(
            List<InvestmentIncomePoint> income,
            DateTime date,
            List<Lot> lots,
            InvestmentBookState state)
        {
            if (!lots.Any(lot => lot.Shares > 0.0000001m))
                return;
            if (income.Count > 0 && income[^1].Date == date.Date)
                return;
            income.Add(new InvestmentIncomePoint
            {
                Date = date.Date,
                EstimatedAnnualIncome = Income(lots, state, date)
            });
        }

        private static decimal Income(List<Lot> lots, InvestmentBookState state, DateTime date)
        {
            decimal total = 0;
            foreach (var group in lots.Where(lot => lot.Shares > 0).GroupBy(lot => lot.Symbol, StringComparer.OrdinalIgnoreCase))
            {
                var rate = LatestQuote(state, group.Key, date)?.AnnualIncomePerShare;
                if (rate is decimal annual)
                    total += group.Sum(lot => lot.Shares) * annual;
            }

            return total;
        }

        private static SecurityQuote? LatestQuote(InvestmentBookState state, string symbol, DateTime date) =>
            state.Quotes
                .Where(quote => SameSymbol(quote.Symbol, symbol) && quote.AsOf.Date <= date.Date)
                .OrderByDescending(quote => quote.AsOf)
                .FirstOrDefault();

        private static InvestmentAction Add(InvestmentBookState state, InvestmentAction action)
        {
            action.AccountId = state.AccountId;
            action.Sequence = state.Actions.Count == 0 ? 1 : state.Actions.Max(existing => existing.Sequence) + 1;
            state.Actions.Add(action);
            return action;
        }

        private static void Remember(InvestmentBookState state, string symbol, string name, bool isSweep)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return;
            var existing = state.Securities.FirstOrDefault(security => SameSymbol(security.Symbol, symbol));
            if (existing is null)
            {
                state.Securities.Add(new InvestmentSecurity { Symbol = symbol, Name = name, IsSweep = isSweep });
                return;
            }

            if (!string.IsNullOrWhiteSpace(name))
                existing.Name = name;
            existing.IsSweep = existing.IsSweep || isSweep;
        }

        private static InvestmentActionKind ClassifyKind(string action, string symbol)
        {
            var text = action.Trim();
            if (Starts(text, "DIVIDEND RECEIVED"))
                return InvestmentActionKind.Dividend;
            if (Starts(text, "INTEREST"))
                return InvestmentActionKind.Interest;
            if (Starts(text, "PARTIC CONTR"))
                return InvestmentActionKind.Contribution;
            if (Starts(text, "NORMAL DISTR"))
                return string.IsNullOrWhiteSpace(symbol) ? InvestmentActionKind.Distribution : InvestmentActionKind.Unknown;
            if (Starts(text, "YOU BOUGHT"))
                return InvestmentActionKind.Buy;
            if (Starts(text, "YOU SOLD"))
                return InvestmentActionKind.Sell;
            if (Starts(text, "REINVESTMENT"))
                return InvestmentActionKind.Reinvestment;
            if (Starts(text, "STOCK SPLIT") || Starts(text, "REVERSE SPLIT"))
                return InvestmentActionKind.Split;
            if (!string.IsNullOrWhiteSpace(symbol) && IsCashIncome(text))
                return InvestmentActionKind.Income;
            return InvestmentActionKind.Unknown;
        }

        private static bool IsCashIncome(string text) =>
            Starts(text, "LONG-TERM CAP GAIN")
            || Starts(text, "SHORT-TERM CAP GAIN")
            || Starts(text, "CAPITAL GAIN")
            || Starts(text, "CAP GAIN")
            || Starts(text, "RETURN OF CAPITAL");

        private static bool Starts(string text, string prefix) =>
            text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        private static decimal? SplitFactor(string action, string description)
        {
            var match = SplitRatio().Match($"{action} {description}");
            if (!match.Success)
                return null;
            var numerator = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var denominator = decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            if (denominator == 0)
                return null;
            var factor = numerator / denominator;
            return factor > 0 ? factor : null;
        }

        private static string ImportKey(FidelityHistoryRow row) =>
            string.Join("|",
                row.RunDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                row.Action.Trim(),
                row.Symbol.Trim(),
                row.Amount?.ToString("0.########", CultureInfo.InvariantCulture) ?? "",
                row.Quantity?.ToString("0.########", CultureInfo.InvariantCulture) ?? "");

        private static string FailureNote(InvestmentAction action) =>
            action.Kind == InvestmentActionKind.Split
                ? "The split could not be applied."
                : "There are not enough shares, so the row was not applied.";

        private static bool IsProcessing(string? text) =>
            string.Equals(text?.Trim(), "Processing", StringComparison.OrdinalIgnoreCase);

        private static decimal? CostPerShare(FidelityPositionRow holding)
        {
            if (holding.AverageCost is decimal average)
                return average;
            if (holding.CostBasisTotal is decimal total && holding.Quantity is decimal quantity && quantity != 0)
                return total / quantity;
            return 0;
        }

        private static bool SameSymbol(string left, string right) =>
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static bool SharesEqual(decimal left, decimal right) =>
            Math.Abs(left - right) < 0.0000001m;

        private static bool MoneyEqual(decimal left, decimal right) =>
            decimal.Round(left, 2) == decimal.Round(right, 2);

        private static Regex SplitRatio() => new(
            @"(\d+(?:\.\d+)?)\s*(?:FOR|:)\s*(\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private sealed class Lot(string symbol, DateTime acquired, decimal shares, decimal costPerShare, int sequence)
        {
            public string Symbol { get; } = symbol;
            public DateTime Acquired { get; } = acquired;
            public decimal Shares { get; set; } = shares;
            public decimal CostPerShare { get; set; } = costPerShare;
            public int Sequence { get; } = sequence;
        }
    }
}
