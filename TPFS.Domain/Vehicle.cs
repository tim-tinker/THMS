namespace TPFS.Domain;

public class Vehicle
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // EV, Hybrid, ICE
    public decimal AnnualCost { get; set; }
    public EnergyAttribution Energy { get; set; }
    public List<MonthlyCost> MonthlyCosts { get; set; } = new();
}
