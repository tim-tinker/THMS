namespace THMS.Domain.Transportation
{
    public class EvChargingSessionVehicleData : MileageRecordBase
    {
        // ---------------------------------------------------------
        // START DATA (captured when charging begins)
        // ---------------------------------------------------------
        public DateTime? StartTimestamp { get; set; }
        public int? StartSocPercent { get; set; }

        // The odometer does not change during charging.
        // This is the ONLY odometer value for the session.
        public decimal? OdometerMiles { get; set; }

        // ---------------------------------------------------------
        // END DATA (captured when charging ends)
        // ---------------------------------------------------------
        // the end timestamp uses the MileageRecordBase.Date property, which is the only date/time at which the session ends.
        public int? EndSocPercent { get; set; }

        // ---------------------------------------------------------
        // OPTIONAL FUTURE FIELDS
        // ---------------------------------------------------------
        // public decimal? StartBatteryTempC { get; set; }
        // public decimal? EndBatteryTempC { get; set; }
    }
}
