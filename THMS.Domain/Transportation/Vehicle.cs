namespace THMS.Domain.Transportation
{
    public class Vehicle : BaseDomainModel
    {
        public string Name { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }

        public bool IsElectric { get; set; }
        public decimal? BatteryCapacityKwh { get; set; }
        public decimal? EfficiencyWhPerMile { get; set; }

        public string? Vin { get; set; }
    }
}
