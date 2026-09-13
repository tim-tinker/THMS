namespace THMS.Domain.Transportation
{
    public class HomeEvChargeSession : BaseEvChargeSession
    {
        // Attribution (null until solar data imported)
        public HomeEvChargeAttribution? Attribution { get; set; }

        // Billing (null until an electric contract covers the session)
        public HomeEvChargeBilling? Billing { get; set; }
    }
}
