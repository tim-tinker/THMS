namespace THMS.Domain.Transportation
{
    public abstract class VehicleBase
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Vin { get; set; } = string.Empty;
    }
}
