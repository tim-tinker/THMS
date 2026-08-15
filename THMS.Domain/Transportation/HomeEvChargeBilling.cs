namespace THMS.Domain.Transportation
{
    public class HomeEvChargeBilling
    {
        public decimal SessionCost { get; set; }

        // Optional metadata
        public decimal GridRate { get; set; }
        public Guid BillingCycleId { get; set; }
    }
}
