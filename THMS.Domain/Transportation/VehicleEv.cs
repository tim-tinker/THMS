namespace THMS.Domain.Transportation
{
    public class VehicleEv : VehicleBase
    {
        public decimal BatteryCapacityKwh { get; set; }
        public string ChargePortType { get; set; } = "J1772";
    }
}
