namespace THMS.Logic.ViewModels.Finance
{
    public class UpcomingObligation
    {
        public Guid AccountId { get; set; }
        public Guid? StatementId { get; set; }
        public string AccountName { get; set; } = "";
        public DateTime DueDate { get; set; }
        public decimal MinimumPayment { get; set; }
        public decimal AmountDue { get; set; }
        public decimal PromotionalDue { get; set; }
        public string Notes { get; set; } = "";
    }
}
