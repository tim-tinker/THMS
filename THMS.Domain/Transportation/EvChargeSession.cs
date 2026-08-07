namespace THMS.Domain.Transportation
{
    public class EvChargeSession : MileageRecordBase
    {
        // Previous session context (must be persisted)
        public decimal LastOdometer { get; set; }
        public decimal LastSoc { get; set; }

        // Basic session data
        public DateTime StartTime { get; set; }
        public decimal StartSoc { get; set; }
        public decimal EndSoc { get; set; }
        public bool IsHomeCharge { get; set; }

        // Source-dependent data (manual for commercial, computed for home)
        public decimal KwhAdded { get; set; }
        public decimal BatteryKwhAdded { get; set; }
        public decimal SessionCost { get; set; }

        // home energy source data (if applicable)
        public decimal GridKwh { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal BatteryKwh { get; set; }
    }
}
