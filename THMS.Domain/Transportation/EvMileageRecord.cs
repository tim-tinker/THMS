namespace THMS.Domain.Transportation
{
    /// <summary>
    /// Represents an EV charging + mileage event.
    /// Includes SOC tracking, charging session details,
    /// and energy delivered.
    /// </summary>
    public class EvMileageRecord : MileageRecordBase
    {
        public decimal StartSocPercent { get; set; }
        public decimal EndSocPercent { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public decimal ChargerPowerKw { get; set; }
        public decimal KwhAdded { get; set; }

        /// <summary>
        /// Cost of the charging session (actual for commercial,
        /// estimated for home charging).
        /// </summary>
        public decimal ChargingCost { get; set; }
    }
}
