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

        public void Import(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var rowIndex = 0;

            while (reader.Read())
            {
                rowIndex++;

                // Skip header row
                if (rowIndex == 1)
                    continue;

                // Expected columns:
                // 0 = Name
                // 1 = Type
                // 2 = Number
                // 3 = CreditLimit
                // 4 = APR
                // 5 = Principal
                // 6 = TermMonths

                var name = reader.GetString(0)?.Trim();
                var type = reader.GetString(1)?.Trim();
                var number = reader.GetString(2)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(type))
                {
                    Console.WriteLine($"Skipping row {rowIndex}: missing name or type.");
                    continue;
                }

                Account account = type switch
                {
                    "Bank" => new BankAccount { Type = AccountType.Checking },
                    "Credit" => new CreditAccount { Type = AccountType.CreditCard },
                    "Loan" => new LoanAccount { Type = AccountType.Loan },
                    "Mortgage" => new MortgageAccount { Type = AccountType.Mortgage },
                    "Investment" => new InvestmentAccount { Type = AccountType.Investment },
                    "Internal" => new InternalAccount { Type = AccountType.Internal },
                    _ => throw new InvalidOperationException(
                        $"Unknown account type '{type}' at row {rowIndex}.")
                };

                account.Id = Guid.NewGuid();
                account.Name = name;
                account.Institution ??= string.Empty;
                account.AccountNumber = number;

                // Subtype-specific fields
                switch (account)
                {
                    case BankAccount bank:
                        break;

                    case CreditAccount credit:
                        if (decimal.TryParse(reader.GetValue(3)?.ToString(),
                            NumberStyles.Any, CultureInfo.InvariantCulture,
                            out var limit))
                            credit.CreditLimit = limit;

                        if (decimal.TryParse(reader.GetValue(4)?.ToString(),
                            NumberStyles.Any, CultureInfo.InvariantCulture,
                            out var apr))
                            credit.APR = apr;
                        break;

                    case LoanAccount loan:
                        if (decimal.TryParse(reader.GetValue(5)?.ToString(),
                            NumberStyles.Any, CultureInfo.InvariantCulture,
                            out var principal))
                            loan.Principal = principal;

                        if (int.TryParse(reader.GetValue(6)?.ToString(), out var term))
                            loan.TermMonths = term;
                        break;

                    case MortgageAccount mortgage:
                        if (decimal.TryParse(reader.GetValue(5)?.ToString(),
                            NumberStyles.Any, CultureInfo.InvariantCulture,
                            out var mPrincipal))
                            mortgage.Principal = mPrincipal;

                        if (int.TryParse(reader.GetValue(6)?.ToString(), out var mTerm))
                            mortgage.TermMonths = mTerm;
                        break;

                    case InternalAccount:
                        // No extra fields
                        break;
                }

                _accountStore.UpsertAccount(account);
            }
        }
    }
}
