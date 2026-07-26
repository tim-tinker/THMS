namespace THMS.Domain.Transportation
{
    public class VehicleIce : VehicleBase
    {
        public decimal FuelTankCapacityGallons { get; set; }
        public string FuelType { get; set; } = "Gasoline";
    }
}
