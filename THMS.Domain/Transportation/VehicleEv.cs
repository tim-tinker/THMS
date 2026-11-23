namespace THMS.Domain.Transportation
{
    public class VehicleEv : VehicleBase
    {
        public decimal BatteryCapacityKwh { get; set; }
        public string ChargePortType { get; set; } = "J1772";

        /// <summary>
        /// Fitted E(s) = A1·s + A2·s² + A3·s³ for displayed SOC percent.
        /// Null until actual charge sessions have been fit.
        /// </summary>
        public SocEnergyCoefficients? ChargeEnergy { get; set; }
    }
}
