using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace THMS.Logic.Finance.Investments
{
    public sealed class FidelityPositionRow
    {
        public string Symbol { get; init; } = "";
        public string Description { get; init; } = "";
        public decimal? Quantity { get; init; }
        public decimal? CurrentValue { get; init; }
        public decimal? LastPrice { get; init; }
        public decimal? CostBasisTotal { get; init; }
        public decimal? AverageCost { get; init; }
        public decimal? AmountPerShare { get; init; }
        public decimal? EstimatedAnnualIncome { get; init; }
        public decimal? DistributionRate { get; init; }
        public DateTime? RateAsOf { get; init; }
        public DateTime? ExDate { get; init; }
        public DateTime? PayDate { get; init; }
        public bool IsCash { get; init; }
    }

    public sealed class FidelityPositionsFile
    {
        public DateTime FileDate { get; init; }
        public string AccountNumber { get; init; } = "";
        public string AccountName { get; init; } = "";
        public decimal? Cash { get; init; }
        public string CashSymbol { get; init; } = "";
        public string CashDescription { get; init; } = "";
        public IReadOnlyList<FidelityPositionRow> Holdings { get; init; } = [];
    }

    public sealed class FidelityHistoryRow
    {
        public DateTime RunDate { get; init; }
        public string Action { get; init; } = "";
        public string Symbol { get; init; } = "";
        public string Description { get; init; } = "";
        public decimal? Price { get; init; }
        public decimal? Quantity { get; init; }
        public decimal? Amount { get; init; }
        public string CashBalanceText { get; init; } = "";
        public decimal? CashBalance { get; init; }
    }

    public sealed class FidelityHistoryFile
    {
        public IReadOnlyList<FidelityHistoryRow> Rows { get; init; } = [];
    }

    public static class FidelityInvestmentFile
    {
        public static FidelityPositionsFile ReadPositions(string path) =>
            ReadPositionAccounts(path).FirstOrDefault() ?? new FidelityPositionsFile();

        public static IReadOnlyList<FidelityPositionsFile> ReadPositionAccounts(string path) =>
            ParsePositionAccounts(File.ReadAllText(path), Path.GetFileName(path));

        public static FidelityHistoryFile ReadHistory(string path) =>
            ParseHistory(File.ReadAllText(path));

        public static FidelityPositionsFile ParsePositions(string csv, string? fileName = null) =>
            ParsePositionAccounts(csv, fileName).FirstOrDefault()
            ?? new FidelityPositionsFile { FileDate = FileDate(fileName, csv, []) };

        public static IReadOnlyList<FidelityPositionsFile> ParsePositionAccounts(string csv, string? fileName = null)
        {
            var rows = ParseCsv(csv);
            var headerIndex = HeaderIndex(rows);
            if (headerIndex < 0)
                throw new InvalidOperationException("The positions file is empty.");

            var columns = Columns(rows[headerIndex]);
            var symbol = Require(columns, "Symbol");
            var description = Optional(columns, "Description");
            var quantity = Optional(columns, "Quantity");
            var currentValue = Optional(columns, "Current value");
            var lastPrice = Optional(columns, "Last price");
            var costBasis = Optional(columns, "Cost basis total");
            var averageCost = Optional(columns, "Average cost basis");
            var amountPerShare = Optional(columns, "Amount per share");
            var estimatedIncome = Optional(columns, "Est. annual income");
            var distRate = Optional(columns, "Dist. rate");
            var rateAsOf = Optional(columns, "Distribution rate as of");
            var exDate = Optional(columns, "Ex-date");
            var payDate = Optional(columns, "Pay date");
            var account = Optional(columns, "Account number");
            var accountName = Optional(columns, "Account name");

            var groups = new List<PositionGroup>();
            PositionGroup? current = null;
            for (var i = headerIndex + 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.All(string.IsNullOrWhiteSpace))
                    continue;

                var accountCell = Cell(row, account);
                if (account >= 0 && !IsAccountNumber(accountCell))
                    break;

                if (current is null || !string.Equals(current.AccountNumber, accountCell, StringComparison.OrdinalIgnoreCase))
                {
                    current = new PositionGroup { AccountNumber = accountCell };
                    groups.Add(current);
                }

                var name = Cell(row, accountName);
                if (current.AccountName.Length == 0 && name.Length > 0)
                    current.AccountName = name;

                var symbolText = InvestmentSymbols.Normalize(Cell(row, symbol));
                var descriptionText = Cell(row, description);
                if (symbolText.Contains("PENDING", StringComparison.OrdinalIgnoreCase)
                    || descriptionText.Contains("PENDING ACTIVITY", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parsed = new FidelityPositionRow
                {
                    Symbol = symbolText,
                    Description = descriptionText,
                    Quantity = Money(Cell(row, quantity)),
                    CurrentValue = Money(Cell(row, currentValue)),
                    LastPrice = Money(Cell(row, lastPrice)),
                    CostBasisTotal = Money(Cell(row, costBasis)),
                    AverageCost = Money(Cell(row, averageCost)),
                    AmountPerShare = Money(Cell(row, amountPerShare)),
                    EstimatedAnnualIncome = Money(Cell(row, estimatedIncome)),
                    DistributionRate = Money(Cell(row, distRate)),
                    RateAsOf = Date(Cell(row, rateAsOf)),
                    ExDate = Date(Cell(row, exDate)),
                    PayDate = Date(Cell(row, payDate)),
                    IsCash = InvestmentSymbols.IsSweep(symbolText, descriptionText)
                };

                if (parsed.IsCash)
                {
                    current.Cash = parsed.CurrentValue ?? parsed.Quantity;
                    current.CashSymbol = parsed.Symbol;
                    current.CashDescription = parsed.Description;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(parsed.Symbol) || parsed.Quantity is null or 0)
                    continue;

                current.Holdings.Add(parsed);
            }

            var holdings = groups.SelectMany(group => group.Holdings).ToList();
            var fileDate = FileDate(fileName, csv, holdings);
            return groups.Select(group => new FidelityPositionsFile
            {
                FileDate = fileDate,
                AccountNumber = group.AccountNumber,
                AccountName = group.AccountName,
                Cash = group.Cash,
                CashSymbol = group.CashSymbol,
                CashDescription = group.CashDescription,
                Holdings = group.Holdings
            }).ToList();
        }

        public static FidelityHistoryFile ParseHistory(string csv)
        {
            var rows = ParseCsv(csv);
            var headerIndex = HeaderIndex(rows);
            if (headerIndex < 0)
                throw new InvalidOperationException("The history file is empty.");

            var columns = Columns(rows[headerIndex]);
            var runDate = Require(columns, "Run Date");
            var action = Require(columns, "Action");
            var symbol = Optional(columns, "Symbol");
            var description = Optional(columns, "Description");
            var price = Optional(columns, "Price ($)", "Price");
            var quantity = Optional(columns, "Quantity");
            var amount = Optional(columns, "Amount ($)", "Amount");
            var cash = Optional(columns, "Cash Balance ($)", "Cash Balance");

            var parsed = new List<FidelityHistoryRow>();
            for (var i = headerIndex + 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.All(string.IsNullOrWhiteSpace))
                    continue;

                var date = Date(Cell(row, runDate));
                if (date is null)
                    break;

                var cashText = Cell(row, cash);
                parsed.Add(new FidelityHistoryRow
                {
                    RunDate = date.Value.Date,
                    Action = Cell(row, action),
                    Symbol = InvestmentSymbols.Normalize(Cell(row, symbol)),
                    Description = Cell(row, description),
                    Price = Money(Cell(row, price)),
                    Quantity = Money(Cell(row, quantity)),
                    Amount = Money(Cell(row, amount)),
                    CashBalanceText = cashText,
                    CashBalance = Money(cashText)
                });
            }

            if (parsed.Count >= 2 && parsed[0].RunDate > parsed[^1].RunDate)
                parsed.Reverse();
            return new FidelityHistoryFile { Rows = parsed };
        }

        private static DateTime FileDate(string? fileName, string csv, IReadOnlyList<FidelityPositionRow> holdings)
        {
            var named = fileName is null ? null : DatedName().Match(fileName);
            if (named is { Success: true } && Date(named.Value) is DateTime fromName)
                return fromName.Date;

            var downloaded = DateDownloaded().Match(csv);
            if (downloaded.Success && Date(downloaded.Groups[1].Value) is DateTime fromFooter)
                return fromFooter.Date;

            DateTime? rate = null;
            foreach (var holding in holdings)
            {
                if (holding.RateAsOf is DateTime asOf && (rate is null || asOf > rate))
                    rate = asOf;
            }

            return rate ?? DateTime.Today;
        }

        private static bool IsAccountNumber(string text) =>
            text.Length is >= 4 and <= 20 && text.All(char.IsAsciiLetterOrDigit);

        private sealed class PositionGroup
        {
            public string AccountNumber { get; init; } = "";
            public string AccountName { get; set; } = "";
            public decimal? Cash { get; set; }
            public string CashSymbol { get; set; } = "";
            public string CashDescription { get; set; } = "";
            public List<FidelityPositionRow> Holdings { get; } = [];
        }

        private static int HeaderIndex(List<List<string>> rows) =>
            rows.FindIndex(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)));

        private static Dictionary<string, int> Columns(List<string> header)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < header.Count; i++)
            {
                var name = header[i].Trim();
                if (name.Length > 0 && !map.ContainsKey(name))
                    map[name] = i;
            }

            return map;
        }

        private static int Require(Dictionary<string, int> columns, string name)
        {
            if (!columns.TryGetValue(name, out var index))
                throw new InvalidOperationException($"The file needs a {name} column.");
            return index;
        }

        private static int Optional(Dictionary<string, int> columns, params string[] names)
        {
            foreach (var name in names)
            {
                if (columns.TryGetValue(name, out var index))
                    return index;
            }

            return -1;
        }

        private static string Cell(List<string> row, int index) =>
            index >= 0 && index < row.Count ? row[index].Trim() : "";

        internal static decimal? Money(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();
            if (text is "--" or "-" or "Processing" or "N/A")
                return null;

            var negative = text.StartsWith('(') && text.EndsWith(')');
            text = text.Trim('(', ')').Replace("$", "").Replace(",", "").Replace("%", "").Trim();
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                return null;
            return negative ? -value : value;
        }

        internal static DateTime? Date(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var formats = new[] { "MM/dd/yyyy", "M/d/yyyy", "MMM-dd-yyyy", "MMM-d-yyyy" };
            if (DateTime.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                return exact.Date;
            return null;
        }

        private static List<List<string>> ParseCsv(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < csv.Length; i++)
            {
                var character = csv[i];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        field.Append(character);
                    }

                    continue;
                }

                switch (character)
                {
                    case '"':
                        quoted = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row);
                        row = [];
                        break;
                    default:
                        field.Append(character);
                        break;
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }

        private static Regex DatedName() => new(
            "(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)-\\d{1,2}-\\d{4}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static Regex DateDownloaded() => new(
            "Date downloaded\\s+([A-Za-z]{3}-\\d{1,2}-\\d{4}|\\d{1,2}/\\d{1,2}/\\d{4})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static class InvestmentSymbols
    {
        private static readonly HashSet<string> Sweeps = new(StringComparer.OrdinalIgnoreCase)
        {
            "FDRXX",
            "SPAXX",
            "FCASH"
        };

        public static string Normalize(string? symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return "";
            return symbol.Trim().TrimEnd('*');
        }

        public static bool IsSweep(string? symbol, string? description)
        {
            if (Sweeps.Contains(Normalize(symbol)))
                return true;
            return description?.Contains("HELD IN ", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
