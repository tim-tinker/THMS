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

        public List<Account> Parse(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = CreateReader(stream, filePath);

            var accounts = new List<Account>();
            var rowIndex = 0;

            while (reader.Read())
            {
                rowIndex++;
                if (rowIndex == 1)
                    continue;

                var parsed = ReadRow(reader, rowIndex);
                if (parsed is not null)
                    accounts.Add(parsed);
            }

            return accounts;
        }

        public void Import(string filePath)
        {
            foreach (var account in Parse(filePath))
                _accountStore.UpsertAccount(account);
        }

        private static IExcelDataReader CreateReader(Stream stream, string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return ExcelReaderFactory.CreateCsvReader(stream);

            return ExcelReaderFactory.CreateReader(stream);
        }

        private static Account? ReadRow(IExcelDataReader reader, int rowIndex)
        {
            var name = ReadString(reader, 0);
            var type = ReadString(reader, 1);
            var number = ReadString(reader, 2);
            var url = ReadString(reader, 3);

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

            switch (account)
            {
                case CreditAccount credit:
                    if (TryReadDecimal(reader, 4, out var limit))
                        credit.CreditLimit = limit;
                    if (TryReadDecimal(reader, 5, out var apr))
                        credit.APR = apr;
                    break;

                case LoanAccount loan:
                    if (TryReadDecimal(reader, 5, out var loanApr))
                        loan.InterestRate = loanApr;
                    if (TryReadDecimal(reader, 6, out var principal))
                        loan.Principal = principal;
                    if (TryReadInt(reader, 7, out var term))
                        loan.TermMonths = term;
                    break;

                case MortgageAccount mortgage:
                    if (TryReadDecimal(reader, 5, out var mortgageApr))
                        mortgage.InterestRate = mortgageApr;
                    if (TryReadDecimal(reader, 6, out var mPrincipal))
                        mortgage.Principal = mPrincipal;
                    if (TryReadInt(reader, 7, out var mTerm))
                        mortgage.TermMonths = mTerm;
                    break;
            }

            return account;
        }

        private static string ReadString(IExcelDataReader reader, int index)
        {
            if (reader.FieldCount <= index)
                return string.Empty;
            return reader.GetValue(index)?.ToString()?.Trim() ?? string.Empty;
        }

        private static bool TryReadDecimal(IExcelDataReader reader, int index, out decimal value)
        {
            value = 0;
            if (reader.FieldCount <= index)
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
            if (reader.FieldCount <= index)
                return false;
            return int.TryParse(reader.GetValue(index)?.ToString(), out value);
        }
    }
}
