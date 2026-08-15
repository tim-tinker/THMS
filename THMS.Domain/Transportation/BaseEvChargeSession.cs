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
    }
}
