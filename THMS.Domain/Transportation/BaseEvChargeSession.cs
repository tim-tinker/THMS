namespace THMS.Domain.Transportation
{
    public class BaseEvChargeSession : MileageRecordBase
    {
        // Previous session context (must be persisted)
        public decimal LastOdometer { get; set; }
        public decimal LastSoc { get; set; }

        // Basic session data
        public DateTime StartTime { get; set; }
        public decimal StartSoc { get; set; }
        public decimal EndSoc { get; set; }

        // Source-dependent data (manual for commercial, computed for home)
        public decimal KwhAdded { get; set; }

        public decimal? KwhDrawn
        {
            get => this is HomeEvChargeSession home && home.Attribution is not null
                ? home.Attribution.TotalKwh
                : field;
            set;
        }

        public decimal SessionCost
        {
            get => this is HomeEvChargeSession home && home.Billing is not null
                ? home.Billing.SessionCost
                : field;
            set;
        }

        public bool IsHomeCharge => this is HomeEvChargeSession;
        public decimal SolarKwh => (this as HomeEvChargeSession)?.Attribution?.SolarKwh ?? 0;
        public decimal BatteryKwh => (this as HomeEvChargeSession)?.Attribution?.BatteryKwh ?? 0;
        public decimal GridKwh => (this as HomeEvChargeSession)?.Attribution?.GridKwh ?? 0;
    }
}
