namespace THMS.Domain.Finance.Investments
{
    public static class InvestmentPositionColumnMapping
    {
        public static InvestmentPositionColumnMap Suggest(Guid accountId, IReadOnlyList<string> headers)
        {
            var symbol = Find(headers, "Symbol", "Ticker", "Fund Name", "Fund", "Security Name", "Security", "Name");
            var description = Find(headers, "Description", "Security Name", "Name");
            if (description is not null && string.Equals(description, symbol, StringComparison.OrdinalIgnoreCase))
                description = null;

            return new InvestmentPositionColumnMap
            {
                AccountId = accountId,
                SymbolColumn = symbol ?? "",
                DescriptionColumn = description ?? "",
                QuantityColumn = Find(headers, "Quantity", "Units", "Shares", "Share Quantity") ?? "",
                PriceColumn = Find(headers, "Last price", "Fund Price", "NAV", "Unit Price", "Unit Value", "Price") ?? "",
                ValueColumn = Find(headers, "Current value", "Closing Balance", "Market Value", "Market value", "Value") ?? "",
                CostBasisColumn = Find(headers, "Cost basis total", "Cost Basis", "Total Cost") ?? "",
                AverageCostColumn = Find(headers, "Average cost basis", "Average Cost") ?? "",
                AccountNumberColumn = Find(headers, "Account number", "Account #", "Account No", "Account No.") ?? "",
                AccountNameColumn = Find(headers, "Account name") ?? "",
                AsOfColumn = Find(headers, "Date Range", "As of", "As-of Date", "Price Date", "Date") ?? "",
                AmountPerShareColumn = Find(headers, "Amount per share") ?? "",
                EstimatedIncomeColumn = Find(headers, "Est. annual income", "Estimated Annual Income") ?? "",
                DistributionRateColumn = Find(headers, "Dist. rate", "Distribution Rate") ?? "",
                RateAsOfColumn = Find(headers, "Distribution rate as of") ?? "",
                ExDateColumn = Find(headers, "Ex-date", "Ex Date") ?? "",
                PayDateColumn = Find(headers, "Pay date", "Pay Date") ?? ""
            };
        }

        public static InvestmentPositionColumnMap Resolve(InvestmentPositionColumnMap? saved, IReadOnlyList<string> headers, Guid accountId)
        {
            if (saved is not null && Fits(saved, headers))
                return saved.ForAccount(accountId);
            return Suggest(accountId, headers);
        }

        public static string? Validate(IReadOnlyList<string> headers, InvestmentPositionColumnMap map)
        {
            ArgumentNullException.ThrowIfNull(headers);
            ArgumentNullException.ThrowIfNull(map);

            if (ColumnIndex(headers, map.SymbolColumn) < 0)
                return "Select the security name or symbol column.";
            if (ColumnIndex(headers, map.QuantityColumn) < 0)
                return "Select the shares column.";
            if (Same(map.SymbolColumn, map.QuantityColumn))
                return "The security and the shares must be different columns.";

            foreach (var name in OptionalColumns(map))
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                if (ColumnIndex(headers, name) < 0)
                    return $"The column \"{name}\" is not in this file.";
            }

            var chosen = new List<string>
            {
                map.SymbolColumn,
                map.QuantityColumn,
                map.PriceColumn,
                map.ValueColumn,
                map.CostBasisColumn,
                map.AverageCostColumn,
                map.AccountNumberColumn,
                map.AccountNameColumn,
                map.AsOfColumn,
                map.AmountPerShareColumn,
                map.EstimatedIncomeColumn,
                map.DistributionRateColumn,
                map.RateAsOfColumn,
                map.ExDateColumn,
                map.PayDateColumn
            };
            if (!string.IsNullOrWhiteSpace(map.DescriptionColumn) && !Same(map.DescriptionColumn, map.SymbolColumn))
                chosen.Add(map.DescriptionColumn);
            var named = chosen.Where(name => !string.IsNullOrWhiteSpace(name));
            if (named.Distinct(StringComparer.OrdinalIgnoreCase).Count() != named.Count())
                return "Each position field needs its own column.";
            return null;
        }

        public static int ColumnIndex(IReadOnlyList<string> headers, string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return -1;

            var wanted = name.Trim();
            for (var i = 0; i < headers.Count; i++)
            {
                if (string.Equals(headers[i], wanted, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static bool Fits(InvestmentPositionColumnMap map, IReadOnlyList<string> headers)
        {
            if (ColumnIndex(headers, map.SymbolColumn) < 0 || ColumnIndex(headers, map.QuantityColumn) < 0)
                return false;
            foreach (var name in OptionalColumns(map))
            {
                if (!string.IsNullOrWhiteSpace(name) && ColumnIndex(headers, name) < 0)
                    return false;
            }

            return true;
        }

        private static IEnumerable<string> OptionalColumns(InvestmentPositionColumnMap map)
        {
            yield return map.DescriptionColumn;
            yield return map.PriceColumn;
            yield return map.ValueColumn;
            yield return map.CostBasisColumn;
            yield return map.AverageCostColumn;
            yield return map.AccountNumberColumn;
            yield return map.AccountNameColumn;
            yield return map.AsOfColumn;
            yield return map.AmountPerShareColumn;
            yield return map.EstimatedIncomeColumn;
            yield return map.DistributionRateColumn;
            yield return map.RateAsOfColumn;
            yield return map.ExDateColumn;
            yield return map.PayDateColumn;
        }

        private static string? Find(IReadOnlyList<string> headers, params string[] names)
        {
            foreach (var name in names)
            {
                foreach (var header in headers)
                {
                    if (string.Equals(header, name, StringComparison.OrdinalIgnoreCase))
                        return header;
                }
            }

            return null;
        }

        private static bool Same(string left, string right) =>
            !string.IsNullOrWhiteSpace(left)
            && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static class InvestmentHistoryColumnMapping
    {
        public static InvestmentHistoryColumnMap Suggest(Guid accountId, IReadOnlyList<string> headers)
        {
            var map = new InvestmentHistoryColumnMap
            {
                AccountId = accountId,
                DateColumn = Find(headers, "Run Date", "Trade Date", "Transaction Date", "Date", "Settlement Date") ?? "",
                ActionColumn = Find(headers, "Action", "Activity", "Transaction", "Description") ?? "",
                SymbolColumn = Find(headers, "Symbol", "Ticker", "Fund Name", "Fund") ?? "",
                DescriptionColumn = Find(headers, "Description", "Details", "Memo") ?? "",
                QuantityColumn = Find(headers, "Quantity", "Units", "Shares") ?? "",
                PriceColumn = Find(headers, "Price ($)", "Price", "Fund Price", "NAV") ?? "",
                AmountColumn = Find(headers, "Amount ($)", "Amount", "Net Amount") ?? "",
                CashBalanceColumn = Find(headers, "Cash Balance ($)", "Cash Balance") ?? ""
            };
            if (Same(map.DescriptionColumn, map.ActionColumn) || Same(map.DescriptionColumn, map.DateColumn))
                map.DescriptionColumn = "";
            if (Same(map.SymbolColumn, map.ActionColumn) || Same(map.SymbolColumn, map.DateColumn))
                map.SymbolColumn = "";
            return map;
        }

        public static InvestmentHistoryColumnMap Resolve(InvestmentHistoryColumnMap? saved, IReadOnlyList<string> headers, Guid accountId)
        {
            if (saved is not null && Fits(saved, headers))
                return saved.ForAccount(accountId);
            return Suggest(accountId, headers);
        }

        public static string? Validate(IReadOnlyList<string> headers, InvestmentHistoryColumnMap map)
        {
            ArgumentNullException.ThrowIfNull(headers);
            ArgumentNullException.ThrowIfNull(map);

            if (ColumnIndex(headers, map.DateColumn) < 0)
                return "Select a date column.";
            if (ColumnIndex(headers, map.ActionColumn) < 0)
                return "Select an action column.";
            if (Same(map.DateColumn, map.ActionColumn))
                return "The date and the action must be different columns.";

            foreach (var name in OptionalColumns(map))
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                if (ColumnIndex(headers, name) < 0)
                    return $"The column \"{name}\" is not in this file.";
            }

            var chosen = OptionalColumns(map)
                .Append(map.DateColumn)
                .Append(map.ActionColumn)
                .Where(name => !string.IsNullOrWhiteSpace(name));
            if (chosen.Distinct(StringComparer.OrdinalIgnoreCase).Count() != chosen.Count())
                return "Each activity field needs its own column.";
            return null;
        }

        public static int ColumnIndex(IReadOnlyList<string> headers, string? name) =>
            InvestmentPositionColumnMapping.ColumnIndex(headers, name);

        private static bool Fits(InvestmentHistoryColumnMap map, IReadOnlyList<string> headers)
        {
            if (ColumnIndex(headers, map.DateColumn) < 0 || ColumnIndex(headers, map.ActionColumn) < 0)
                return false;
            foreach (var name in OptionalColumns(map))
            {
                if (!string.IsNullOrWhiteSpace(name) && ColumnIndex(headers, name) < 0)
                    return false;
            }

            return true;
        }

        private static IEnumerable<string> OptionalColumns(InvestmentHistoryColumnMap map)
        {
            yield return map.SymbolColumn;
            yield return map.DescriptionColumn;
            yield return map.QuantityColumn;
            yield return map.PriceColumn;
            yield return map.AmountColumn;
            yield return map.CashBalanceColumn;
        }

        private static string? Find(IReadOnlyList<string> headers, params string[] names)
        {
            foreach (var name in names)
            {
                foreach (var header in headers)
                {
                    if (string.Equals(header, name, StringComparison.OrdinalIgnoreCase))
                        return header;
                }
            }

            return null;
        }

        private static bool Same(string left, string right) =>
            !string.IsNullOrWhiteSpace(left)
            && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
