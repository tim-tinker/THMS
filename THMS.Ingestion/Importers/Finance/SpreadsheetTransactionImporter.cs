using System.Globalization;
using ExcelDataReader;
using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Ingestion.Importers.Finance
{
    public sealed class ParsedSpreadsheetTransaction
    {
        public PostedTransaction Transaction { get; init; } = null!;
        public string AccountName { get; init; } = "";
        public string CategoryName { get; init; } = "";
    }

    public class SpreadsheetTransactionImporter
    {
        private readonly ITransactionDataStore _transactionStore;
        private readonly ICategoryDataStore _categoryStore;
        private readonly IAccountDataStore _accountStore;

        public SpreadsheetTransactionImporter()
            : this(new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetAccountStore())
        {
        }

        public SpreadsheetTransactionImporter(
            ITransactionDataStore transactionStore,
            IAccountDataStore accountStore)
        {
            _transactionStore = transactionStore;
            _accountStore = accountStore;
            _categoryStore = transactionStore as ICategoryDataStore ?? new DataStoreFactory().GetCategoryStore();

            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);
        }

        public List<ParsedSpreadsheetTransaction> Parse(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = CreateReader(stream, filePath);

            var rows = new List<ParsedSpreadsheetTransaction>();
            var rowIndex = 0;

            while (reader.Read())
            {
                rowIndex++;
                if (rowIndex == 1)
                    continue;

                var parsed = ReadRow(reader, rowIndex);
                if (parsed is not null)
                    rows.Add(parsed);
            }

            return rows;
        }

        private static IExcelDataReader CreateReader(Stream stream, string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return ExcelReaderFactory.CreateCsvReader(stream);

            return ExcelReaderFactory.CreateReader(stream);
        }

        public void Import(string filePath)
        {
            foreach (var parsed in Parse(filePath))
            {
                parsed.Transaction.ApplyCategory(ResolveCategory(parsed.CategoryName));
                _transactionStore.AddPostedTransaction(parsed.Transaction);
            }
        }

        private ParsedSpreadsheetTransaction? ReadRow(IExcelDataReader reader, int rowIndex)
        {
            var category = ReadString(reader, 0);
            var accountName = ReadString(reader, 1);
            var dateString = ReadString(reader, 2);
            var amountString = ReadString(reader, 3);
            var description = ReadString(reader, 4);

            if (string.IsNullOrWhiteSpace(accountName))
                return null;

            var account = _accountStore.GetAccount(accountName);
            if (account is null)
            {
                Console.WriteLine($"Skipping row {rowIndex}: account '{accountName}' not found.");
                return null;
            }

            if (!DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && !DateTime.TryParse(dateString, out date))
                return null;

            if (!decimal.TryParse(amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
                && !decimal.TryParse(amountString, NumberStyles.Any, CultureInfo.CurrentCulture, out amount))
                return null;

            return new ParsedSpreadsheetTransaction
            {
                AccountName = accountName,
                CategoryName = category,
                Transaction = new PostedTransaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    Date = date,
                    Amount = amount,
                    Description = description
                }
            };
        }

        private static string ReadString(IExcelDataReader reader, int index)
        {
            if (index >= reader.FieldCount)
                return "";
            return reader.GetValue(index)?.ToString()?.Trim() ?? "";
        }

        private ExpenseCategory ResolveCategory(string? name)
        {
            _categoryStore.EnsureDefaultCategories();
            var canonical = string.IsNullOrWhiteSpace(name)
                ? DefaultExpenseCategories.Uncategorized
                : DefaultExpenseCategories.CanonicalName(name);
            var existing = _categoryStore.GetAllCategories(includeInactive: true)
                .FirstOrDefault(c => string.Equals(c.Name, canonical, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
                return existing;

            var created = new ExpenseCategory
            {
                Name = canonical,
                IsActive = true,
                DisplayOrder = 200
            };
            _categoryStore.AddCategory(created);
            return created;
        }
    }
}
