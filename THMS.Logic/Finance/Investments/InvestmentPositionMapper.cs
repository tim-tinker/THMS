using System.Globalization;
using System.Text.RegularExpressions;
using THMS.Domain.Finance.Investments;

namespace THMS.Logic.Finance.Investments
{
    public static class InvestmentPositionMapper
    {
        public static IReadOnlyList<FidelityPositionsFile> Parse(InvestmentFileSheet sheet, InvestmentPositionColumnMap map, string? fileName = null)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(map);
            var error = InvestmentPositionColumnMapping.Validate(sheet.Headers, map);
            if (error is not null)
                throw new InvalidOperationException(error);

            var symbol = Index(sheet, map.SymbolColumn);
            var description = Index(sheet, map.DescriptionColumn);
            var quantity = Index(sheet, map.QuantityColumn);
            var price = Index(sheet, map.PriceColumn);
            var value = Index(sheet, map.ValueColumn);
            var cost = Index(sheet, map.CostBasisColumn);
            var average = Index(sheet, map.AverageCostColumn);
            var account = Index(sheet, map.AccountNumberColumn);
            var accountName = Index(sheet, map.AccountNameColumn);
            var asOf = Index(sheet, map.AsOfColumn);
            var amountPerShare = Index(sheet, map.AmountPerShareColumn);
            var income = Index(sheet, map.EstimatedIncomeColumn);
            var rate = Index(sheet, map.DistributionRateColumn);
            var rateAsOf = Index(sheet, map.RateAsOfColumn);
            var exDate = Index(sheet, map.ExDateColumn);
            var payDate = Index(sheet, map.PayDateColumn);

            var groups = new List<Group>();
            Group? current = null;
            var asOfDates = new List<DateTime>();
            foreach (var row in sheet.Rows)
            {
                var accountCell = Cell(row, account);
                if (account >= 0 && !IsAccountNumber(accountCell))
                    break;

                var asOfDate = InvestmentFileDates.ParseAsOf(Cell(row, asOf));
                if (asOfDate is DateTime parsedAsOf)
                    asOfDates.Add(parsedAsOf);

                if (current is null || !string.Equals(current.AccountNumber, accountCell, StringComparison.OrdinalIgnoreCase))
                {
                    current = new Group { AccountNumber = account >= 0 ? accountCell : "" };
                    groups.Add(current);
                }

                var name = Cell(row, accountName);
                if (current.AccountName.Length == 0 && name.Length > 0)
                    current.AccountName = name;

                var symbolText = InvestmentSymbols.Normalize(Cell(row, symbol));
                var descriptionText = description >= 0 ? Cell(row, description) : Cell(row, symbol);
                if (symbolText.Contains("PENDING", StringComparison.OrdinalIgnoreCase)
                    || descriptionText.Contains("PENDING ACTIVITY", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parsed = new FidelityPositionRow
                {
                    Symbol = symbolText,
                    Description = descriptionText,
                    Quantity = FidelityInvestmentFile.Money(Cell(row, quantity)),
                    CurrentValue = FidelityInvestmentFile.Money(Cell(row, value)),
                    LastPrice = FidelityInvestmentFile.Money(Cell(row, price)),
                    CostBasisTotal = FidelityInvestmentFile.Money(Cell(row, cost)),
                    AverageCost = FidelityInvestmentFile.Money(Cell(row, average)),
                    AmountPerShare = FidelityInvestmentFile.Money(Cell(row, amountPerShare)),
                    EstimatedAnnualIncome = FidelityInvestmentFile.Money(Cell(row, income)),
                    DistributionRate = FidelityInvestmentFile.Money(Cell(row, rate)),
                    RateAsOf = InvestmentFileDates.ParseDate(Cell(row, rateAsOf)),
                    ExDate = InvestmentFileDates.ParseDate(Cell(row, exDate)),
                    PayDate = InvestmentFileDates.ParseDate(Cell(row, payDate)),
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

            if (groups.Count == 0)
                groups.Add(new Group());

            var fileDate = InvestmentFileDates.FileDate(fileName, sheet, asOfDates);
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

        private static int Index(InvestmentFileSheet sheet, string name) =>
            InvestmentPositionColumnMapping.ColumnIndex(sheet.Headers, name);

        private static string Cell(InvestmentFileRawRow row, int index) =>
            index >= 0 && index < row.Cells.Count ? row.Cells[index].Trim() : "";

        private static bool IsAccountNumber(string text) =>
            text.Length is >= 4 and <= 20 && text.All(char.IsAsciiLetterOrDigit);

        private sealed class Group
        {
            public string AccountNumber { get; init; } = "";
            public string AccountName { get; set; } = "";
            public decimal? Cash { get; set; }
            public string CashSymbol { get; set; } = "";
            public string CashDescription { get; set; } = "";
            public List<FidelityPositionRow> Holdings { get; } = [];
        }
    }

    public static class InvestmentHistoryMapper
    {
        public static FidelityHistoryFile Parse(InvestmentFileSheet sheet, InvestmentHistoryColumnMap map)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(map);
            var error = InvestmentHistoryColumnMapping.Validate(sheet.Headers, map);
            if (error is not null)
                throw new InvalidOperationException(error);

            var date = InvestmentHistoryColumnMapping.ColumnIndex(sheet.Headers, map.DateColumn);
            var action = InvestmentHistoryColumnMapping.ColumnIndex(sheet.Headers, map.ActionColumn);
            var symbol = InvestmentHistoryColumnMapping.ColumnIndex(sheet.Headers, map.SymbolColumn);
            var description = InvestmentHistoryColumnMapping.ColumnIndex(sheet.Headers, map.DescriptionColumn);
            var price = InvestmentHistoryColumnMapping.ColumnIndex(sheet.Headers, map.PriceColumn);
            var quantity = InvestmentHistoryColumnMapping.ColumnIndex(sheet.Headers, map.QuantityColumn);
            var amount = InvestmentHistoryColumnMapping.ColumnIndex(sheet.Headers, map.AmountColumn);
            var cash = InvestmentHistoryColumnMapping.ColumnIndex(sheet.Headers, map.CashBalanceColumn);

            var parsed = new List<FidelityHistoryRow>();
            foreach (var row in sheet.Rows)
            {
                var runDate = InvestmentFileDates.ParseDate(Cell(row, date));
                if (runDate is null)
                    break;

                var cashText = Cell(row, cash);
                parsed.Add(new FidelityHistoryRow
                {
                    RunDate = runDate.Value.Date,
                    Action = Cell(row, action),
                    Symbol = InvestmentSymbols.Normalize(Cell(row, symbol)),
                    Description = Cell(row, description),
                    Price = FidelityInvestmentFile.Money(Cell(row, price)),
                    Quantity = FidelityInvestmentFile.Money(Cell(row, quantity)),
                    Amount = FidelityInvestmentFile.Money(Cell(row, amount)),
                    CashBalanceText = cashText,
                    CashBalance = FidelityInvestmentFile.Money(cashText)
                });
            }

            if (parsed.Count >= 2 && parsed[0].RunDate > parsed[^1].RunDate)
                parsed.Reverse();
            return new FidelityHistoryFile { Rows = parsed };
        }

        private static string Cell(InvestmentFileRawRow row, int index) =>
            index >= 0 && index < row.Cells.Count ? row.Cells[index].Trim() : "";
    }

    public static class InvestmentFileDates
    {
        public static DateTime? ParseAsOf(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var range = Range().Match(text.Trim());
            if (!range.Success)
                return ParseDate(text);

            return ParseDate(range.Groups[2].Value) ?? ParseDate(range.Groups[1].Value);
        }

        public static DateTime? ParseDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var formats = new[]
            {
                "MM/dd/yyyy",
                "M/d/yyyy",
                "MMM-dd-yyyy",
                "MMM-d-yyyy",
                "MM-dd-yyyy",
                "M-d-yyyy",
                "yyyy-MM-dd",
                "MMMM d, yyyy",
                "MMMM dd, yyyy",
                "MMM d, yyyy",
                "MMM dd, yyyy"
            };
            if (DateTime.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                return exact.Date;
            return null;
        }

        public static DateTime FileDate(string? fileName, InvestmentFileSheet sheet, IReadOnlyList<DateTime> columnDates)
        {
            if (columnDates.Count > 0)
                return columnDates.Max().Date;

            var named = DatesIn(fileName);
            if (named.Count > 0)
                return named.Max().Date;

            foreach (var row in sheet.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    var downloaded = Downloaded().Match(cell);
                    if (downloaded.Success && ParseDate(downloaded.Groups[1].Value) is DateTime fromFooter)
                        return fromFooter.Date;
                }
            }

            return DateTime.Today;
        }

        private static List<DateTime> DatesIn(string? fileName)
        {
            var found = new List<DateTime>();
            if (string.IsNullOrWhiteSpace(fileName))
                return found;

            foreach (Match match in DatedName().Matches(fileName))
            {
                if (ParseDate(match.Value) is DateTime date)
                    found.Add(date);
            }

            return found;
        }

        private static Regex Range() => new(
            @"^(.+?)\s+[-–—]\s+(.+)$",
            RegexOptions.CultureInvariant);

        private static Regex DatedName() => new(
            @"(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)-\d{1,2}-\d{4}|\d{1,2}-\d{1,2}-\d{4}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static Regex Downloaded() => new(
            @"Date downloaded\s+([A-Za-z]{3}-\d{1,2}-\d{4}|\d{1,2}/\d{1,2}/\d{4})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
