using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ExcelDataReader;

using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Ingestion.Importers.Finance
{
    public class SpreadsheetTransactionImporter
    {
        private readonly ITransactionDataStore _transactionStore;
        private readonly IAccountDataStore _accountStore;

        public SpreadsheetTransactionImporter(
            ITransactionDataStore transactionStore,
            IAccountDataStore accountStore)
        {
            _transactionStore = transactionStore;
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
                // 0 = Category
                // 1 = Account
                // 2 = Date
                // 3 = Amount
                // 4 = Transaction (description)

                var category = reader.GetString(0)?.Trim();
                var accountName = reader.GetString(1)?.Trim();
                var dateString = reader.GetValue(2)?.ToString()?.Trim();
                var amountString = reader.GetValue(3)?.ToString()?.Trim();
                var description = reader.GetString(4)?.Trim();

                if (string.IsNullOrWhiteSpace(accountName))
                    continue;

                var account = _accountStore.GetAccountByName(accountName);
                if (account is null)
                {
                    Console.WriteLine(
                        $"Skipping row {rowIndex}: account '{accountName}' not found.");
                    continue;
                }

                if (!DateTime.TryParse(dateString, out var date))
                    continue;

                if (!decimal.TryParse(amountString, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var amount))
                    continue;

                var posted = new PostedTransaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    Date = date,
                    Amount = amount,
                    Category = category ?? "Uncategorized",
                    Description = description ?? string.Empty
                };

                _transactionStore.AddPostedTransaction(posted);
            }
        }
    }
}
