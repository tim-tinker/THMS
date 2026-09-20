using THMS.Domain.Finance.Transactions;
using THMS.Ingestion.Importers.Finance;

namespace THMS.Logic.ViewModels.Finance
{
    public class RecurringRuleImportPreview
    {
        public string Account { get; set; } = "";
        public Guid AccountId { get; set; }
        public string Description { get; set; } = "";
        public string Frequency { get; set; } = "";
        public DateTime? LastOccurrence { get; set; }
        public DateTime NextOccurrence { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = "";

        public static RecurringRuleImportPreview FromParsed(ParsedSpreadsheetRecurringRule row) =>
            new()
            {
                Account = row.AccountName,
                AccountId = row.AccountId,
                Description = row.Description,
                Frequency = SpreadsheetRecurringRuleImporter.FrequencyLabel(row.Frequency),
                LastOccurrence = row.LastOccurrence,
                NextOccurrence = row.NextOccurrence,
                Amount = row.Amount,
                Category = row.CategoryName
            };
    }
}
