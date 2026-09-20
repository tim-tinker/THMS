using THMS.Ingestion.Importers.Finance;

namespace THMS.Logic.ViewModels.Finance
{
    public class RecurringTransferImportPreview
    {
        public string FromAccount { get; set; } = "";
        public Guid FromAccountId { get; set; }
        public string ToAccount { get; set; } = "";
        public Guid ToAccountId { get; set; }
        public string Description { get; set; } = "";
        public string Frequency { get; set; } = "";
        public DateTime? LastOccurrence { get; set; }
        public DateTime NextOccurrence { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = "";

        public static RecurringTransferImportPreview FromParsed(ParsedSpreadsheetRecurringTransfer row) =>
            new()
            {
                FromAccount = row.FromAccountName,
                FromAccountId = row.FromAccountId,
                ToAccount = row.ToAccountName,
                ToAccountId = row.ToAccountId,
                Description = row.Description,
                Frequency = SpreadsheetRecurringRuleImporter.FrequencyLabel(row.Frequency),
                LastOccurrence = row.LastOccurrence,
                NextOccurrence = row.NextOccurrence,
                Amount = row.Amount,
                Category = row.CategoryName
            };
    }
}
