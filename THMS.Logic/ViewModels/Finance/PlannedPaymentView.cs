namespace THMS.Logic.ViewModels.Finance
{
    public class PlannedPaymentView
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public decimal PlannedAmount { get; set; }
        public DateTime PlannedDate { get; set; }
        public string Notes { get; set; } = "";
        public bool IsRealized { get; set; }
        public Guid? StatementId { get; set; }
    }
}
