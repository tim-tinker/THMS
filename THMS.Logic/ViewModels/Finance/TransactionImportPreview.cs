namespace THMS.Logic.ViewModels.Finance
{
    public class TransactionImportPreview
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public string Account { get; set; } = "";
        public string Category { get; set; } = "";
        public Guid AccountId { get; set; }
    }
}
