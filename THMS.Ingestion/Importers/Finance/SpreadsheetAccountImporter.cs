using ExcelDataReader;
using System.Globalization;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;

namespace THMS.Ingestion.Importers.Finance
{
    public class SpreadsheetAccountImporter
    {
        private readonly IAccountDataStore _accountStore;

        public SpreadsheetAccountImporter()
            : this(new DataStoreFactory().GetAccountStore())
        {
        }

        public SpreadsheetAccountImporter(IAccountDataStore accountStore)
        {
            _accountStore = accountStore;

            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);
        }

        public List<ParsedSpreadsheetAccount> Parse(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = CreateReader(stream, filePath);

            var accounts = new List<ParsedSpreadsheetAccount>();
            Dictionary<string, int>? headers = null;
            var rowIndex = 0;

            while (reader.Read())
            {
                rowIndex++;
                if (headers is null)
                {
                    if (IsHeaderRow(reader))
                    {
                        headers = ReadHeaders(reader);
                        continue;
                    }

                    headers = PositionalHeaders(reader.FieldCount);
                }

                var parsed = ReadRow(reader, headers, rowIndex);
                if (parsed is not null)
                    accounts.Add(parsed);
            }

            return accounts;
        }

        public void Import(string filePath)
        {
            var parsed = Parse(filePath);
            foreach (var row in parsed)
                _accountStore.UpsertAccount(row.Account);

            ApplyFundingAccounts(parsed);
        }

        private void ApplyFundingAccounts(IEnumerable<ParsedSpreadsheetAccount> rows)
        {
            foreach (var row in rows)
            {
                if (!row.HasFundingAccount || !row.Account.SupportsAutoPay)
                    continue;

                if (string.IsNullOrWhiteSpace(row.FundingAccountName))
                {
                    row.Account.AutoPayFromAccountId = null;
                }
                else
                {
                    var funding = _accountStore.GetAllAccounts().FirstOrDefault(account =>
                        string.Equals(account.Name, row.FundingAccountName, StringComparison.OrdinalIgnoreCase));
                    if (funding is null)
                        throw new InvalidOperationException(
                            $"Funding account '{row.FundingAccountName}' for '{row.Account.Name}' was not found.");
                    if (funding.Id == row.Account.Id)
                        throw new InvalidOperationException($"'{row.Account.Name}' cannot pay from itself.");
                    row.Account.AutoPayFromAccountId = funding.Id;
                }

                _accountStore.UpsertAccount(row.Account);
            }
        }

        private static IExcelDataReader CreateReader(Stream stream, string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return ExcelReaderFactory.CreateCsvReader(stream);

            return ExcelReaderFactory.CreateReader(stream);
        }

        private static ParsedSpreadsheetAccount? ReadRow(
            IExcelDataReader reader,
            IReadOnlyDictionary<string, int> headers,
            int rowIndex)
        {
            var name = ReadString(reader, Column(headers, "Name"));
            var type = ReadString(reader, Column(headers, "Type"));
            var number = ReadString(reader, Column(headers, "Number", "AccountNumber"));
            var url = ReadString(reader, Column(headers, "URL", "Website", "WebsiteUrl"));

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
                return null;

            Account account = type switch
            {
                "Bank" => new BankAccount { Type = AccountType.Checking },
                "Credit" => new CreditAccount { Type = AccountType.CreditCard },
                "Loan" => new LoanAccount { Type = AccountType.Loan },
                "Mortgage" => new MortgageAccount { Type = AccountType.Mortgage },
                "Investment" => new InvestmentAccount { Type = AccountType.Investment },
                "Internal" => new InternalAccount { Type = AccountType.Internal },
                "Utility" => new UntrackedAccount { Type = AccountType.Utility },
                "Service" => new UntrackedAccount { Type = AccountType.Service },
                "Insurance" => new UntrackedAccount { Type = AccountType.Insurance },
                _ => throw new InvalidOperationException(
                    $"Unknown account type '{type}' at row {rowIndex}.")
            };

            account.Id = Guid.NewGuid();
            account.Name = name;
            account.Institution ??= string.Empty;
            account.AccountNumber = number;
            account.WebsiteUrl = url;

            var creditLimitColumn = Column(headers, "CreditLimit");
            var aprColumn = Column(headers, "APR", "InterestRate");
            var principalColumn = Column(headers, "Principal");
            var termColumn = Column(headers, "TermMonths", "Term");
            switch (account)
            {
                case CreditAccount credit:
                    if (TryReadDecimal(reader, creditLimitColumn, out var limit))
                        credit.CreditLimit = limit;
                    if (TryReadDecimal(reader, aprColumn, out var apr))
                        credit.APR = apr;
                    break;

                case LoanAccount loan:
                    if (TryReadDecimal(reader, aprColumn, out var loanApr))
                        loan.InterestRate = loanApr;
                    if (TryReadDecimal(reader, principalColumn, out var principal))
                        loan.Principal = -principal;
                    if (TryReadInt(reader, termColumn, out var term))
                        loan.TermMonths = term;
                    break;

                case MortgageAccount mortgage:
                    if (TryReadDecimal(reader, aprColumn, out var mortgageApr))
                        mortgage.InterestRate = mortgageApr;
                    if (TryReadDecimal(reader, principalColumn, out var mPrincipal))
                        mortgage.Principal = -mPrincipal;
                    if (TryReadInt(reader, termColumn, out var mTerm))
                        mortgage.TermMonths = mTerm;
                    break;
            }

            var autoPayColumn = Column(headers, "AutoPay", "Auto Pay");
            if (autoPayColumn >= 0 && account.SupportsAutoPay)
                account.AutoPay = ReadBool(reader, autoPayColumn);

            var fundingColumn = Column(headers, "FundingAccount", "Funding Account", "PayFrom", "Pay From");
            return new ParsedSpreadsheetAccount
            {
                Account = account,
                HasAutoPay = autoPayColumn >= 0,
                HasFundingAccount = fundingColumn >= 0,
                FundingAccountName = fundingColumn >= 0 ? BlankToNull(ReadString(reader, fundingColumn)) : null
            };
        }

        private static bool IsHeaderRow(IExcelDataReader reader)
        {
            var values = Enumerable.Range(0, reader.FieldCount).Select(index => ReadString(reader, index)).ToList();
            return values.Any(value => Matches(value, "Name")) && values.Any(value => Matches(value, "Type"));
        }

        private static Dictionary<string, int> ReadHeaders(IExcelDataReader reader)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = ReadString(reader, i);
                if (name.Length > 0 && !headers.ContainsKey(name))
                    headers[name] = i;
            }

            return headers;
        }

        private static Dictionary<string, int> PositionalHeaders(int fieldCount)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = 0,
                ["Type"] = 1,
                ["Number"] = 2,
                ["URL"] = 3,
                ["CreditLimit"] = 4,
                ["APR"] = 5,
                ["Principal"] = 6,
                ["TermMonths"] = 7,
                ["FundingAccount"] = 8,
                ["AutoPay"] = 9
            };
            return headers.Where(pair => pair.Value < Math.Max(fieldCount, 1))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static int Column(IReadOnlyDictionary<string, int> headers, params string[] names)
        {
            foreach (var name in names)
            {
                if (headers.TryGetValue(name, out var index))
                    return index;
            }

            return -1;
        }

        private static bool Matches(string? value, params string[] expected) =>
            expected.Any(name => string.Equals(value?.Trim(), name, StringComparison.OrdinalIgnoreCase));

        private static bool ReadBool(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return false;

            return reader.GetValue(index) switch
            {
                bool value => value,
                double value => value != 0,
                int value => value != 0,
                long value => value != 0,
                string text when bool.TryParse(text, out var parsed) => parsed,
                string text => text.Trim().ToLowerInvariant() is "1" or "y" or "yes",
                _ => false
            };
        }

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string ReadString(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return string.Empty;
            return reader.GetValue(index)?.ToString()?.Trim() ?? string.Empty;
        }

        private static bool TryReadDecimal(IExcelDataReader reader, int index, out decimal value)
        {
            value = 0;
            if (index < 0 || index >= reader.FieldCount)
                return false;
            return decimal.TryParse(
                reader.GetValue(index)?.ToString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static bool TryReadInt(IExcelDataReader reader, int index, out int value)
        {
            value = 0;
            if (index < 0 || index >= reader.FieldCount)
                return false;
            return int.TryParse(reader.GetValue(index)?.ToString(), out value);
        }
    }

    public sealed class ParsedSpreadsheetAccount
    {
        public required Account Account { get; init; }
        public bool HasAutoPay { get; init; }
        public bool HasFundingAccount { get; init; }
        public string? FundingAccountName { get; init; }
    }
}
