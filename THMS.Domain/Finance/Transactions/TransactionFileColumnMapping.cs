namespace THMS.Domain.Finance.Transactions
{
    public static class TransactionFileColumnMapping
    {
        public static TransactionFileColumnMap Suggest(Guid accountId, IReadOnlyList<string> headers)
        {
            var date = Find(headers, "Date", "Transaction Date", "Posted Date", "Posting Date", "Post Date", "Trans Date");
            var description = Find(headers, "Description", "Memo", "Details", "Narrative", "Transaction Description", "Name");
            var amount = Find(headers, "Amount", "Transaction Amount");
            var debit = Find(headers, "Debit", "Debits", "Withdrawal", "Withdrawals", "Debit Amount");
            var credit = Find(headers, "Credit", "Credits", "Deposit", "Deposits", "Credit Amount");

            return new TransactionFileColumnMap
            {
                AccountId = accountId,
                DateColumn = date ?? "",
                DescriptionColumn = description ?? "",
                UseDebitCredit = amount is null && debit is not null && credit is not null,
                AmountColumn = amount ?? "",
                DebitColumn = debit ?? "",
                CreditColumn = credit ?? ""
            };
        }

        public static string? Validate(IReadOnlyList<string> headers, TransactionFileColumnMap map)
        {
            ArgumentNullException.ThrowIfNull(headers);
            ArgumentNullException.ThrowIfNull(map);

            if (ColumnIndex(headers, map.DateColumn) < 0)
                return "Select a date column.";
            if (ColumnIndex(headers, map.DescriptionColumn) < 0)
                return "Select a description column.";

            if (map.UseDebitCredit)
            {
                if (ColumnIndex(headers, map.DebitColumn) < 0 || ColumnIndex(headers, map.CreditColumn) < 0)
                    return "Select both a debit column and a credit column.";

                var names = new[] { map.DateColumn, map.DescriptionColumn, map.DebitColumn, map.CreditColumn };
                if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
                    return "Date, description, debit, and credit must be different columns.";
                return null;
            }

            if (ColumnIndex(headers, map.AmountColumn) < 0)
                return "Select an amount column.";

            var amountNames = new[] { map.DateColumn, map.DescriptionColumn, map.AmountColumn };
            if (amountNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
                return "Date, description, and amount must be different columns.";
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
    }
}
