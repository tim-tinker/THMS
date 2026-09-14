namespace THMS.Domain.Transportation
{
    public class HomeEvChargeSession : BaseEvChargeSession
    {
        // Attribution is computed from circuit draw; estimated when solar data is missing.
        public HomeEvChargeAttribution? Attribution { get; set; }

        // Billing (null until an electric contract covers the session)
        public HomeEvChargeBilling? Billing { get; set; }
    }
}
