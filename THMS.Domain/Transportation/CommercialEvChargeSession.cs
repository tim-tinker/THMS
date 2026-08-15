namespace THMS.Domain.Transportation
{
    public class CommercialEvChargeSession : BaseEvChargeSession
    {
        // Vendor-reported kWh drawn
        public decimal KwhDrawn { get; set; }

        // Vendor-reported cost
        public decimal SessionCost { get; set; }
    }
}
