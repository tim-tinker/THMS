namespace THMS.Domain.Transportation
{
    public class EvChargingSession
    {
        public Guid Id { get; set; }
        public Guid VehicleId { get; set; }

        // Previous session context (must be persisted)
        public decimal LastOdometer { get; set; }
        public decimal LastSoc { get; set; }

        // Basic session data
        public decimal OdometerMiles { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal StartSoc { get; set; }
        public decimal EndSoc { get; set; }
        public bool IsHomeCharging { get; set; }

        // Source-dependent data (manual for commercial, computed for home)
        public decimal KwhAdded { get; set; }
        public decimal ChargingCost { get; set; }

        // home energy source data (if applicable)
        public decimal GridKwh { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal BatteryKwh { get; set; }
    }
}
