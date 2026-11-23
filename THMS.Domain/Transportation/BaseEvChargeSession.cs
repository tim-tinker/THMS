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

        // Battery energy is stored on its own row so a refit can replace calculated kWh
        // without rewriting the session.
        public EvBatteryCharge BatteryCharge { get; set; } = new();

        public decimal KwhAdded
        {
            get => BatteryCharge.KwhAdded;
            set => BatteryCharge.KwhAdded = value;
        }

        public bool KwhAddedIsCalculated
        {
            get => BatteryCharge.IsCalculated;
            set => BatteryCharge.IsCalculated = value;
        }

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
        public bool HasCircuitData { get; set; }
        public bool HasSolarData { get; set; }
        public bool HasElectricContract { get; set; }
        public bool DrawnKwhIsUnavailable => IsHomeCharge && !HasCircuitData;
        public bool SourceSplitIsEstimated => IsHomeCharge && HasCircuitData && !HasSolarData;
        public decimal SolarKwh => (this as HomeEvChargeSession)?.Attribution?.SolarKwh ?? 0;
        public decimal BatteryKwh => (this as HomeEvChargeSession)?.Attribution?.BatteryKwh ?? 0;
        public decimal GridKwh => (this as HomeEvChargeSession)?.Attribution?.GridKwh ?? 0;
    }
}
