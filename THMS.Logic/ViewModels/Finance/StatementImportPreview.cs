using THMS.Domain.Finance.Planning;

namespace THMS.Logic.ViewModels.Finance
{
    public class StatementImportPreview
    {
        public string Account { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime StatementDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal AmountDue { get; set; }
        public decimal StatementBalance { get; set; }
        public decimal EscrowBalance { get; set; }
        public string Notes { get; set; } = "";
        public string PayFrom { get; set; } = "";
        public Guid AccountId { get; set; }
        public Guid? PayFromAccountId { get; set; }
        public StatementType StatementType { get; set; }
    }
}
