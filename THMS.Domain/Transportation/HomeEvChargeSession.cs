namespace THMS.Domain.Transportation
{
    public class HomeEvChargeSession : BaseEvChargeSession
    {
        // Circuit-derived kWh drawn (null until circuit data imported)
        public decimal? KwhDrawn { get; set; }

        // Attribution (null until solar data imported)
        public HomeEvChargeAttribution? Attribution { get; set; }

        // Billing (null until utility bill imported)
        public HomeEvChargeBilling? Billing { get; set; }
    }
}
