namespace THMS.Domain.Transportation
{
    public class HomeEvChargeAttribution
    {
        public decimal GridKwh { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal BatteryKwh { get; set; }

        public decimal TotalKwh =>
            GridKwh + SolarKwh + BatteryKwh;
    }
}
