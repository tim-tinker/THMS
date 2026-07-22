namespace THMS.Domain;

public class Vehicle : BaseDomainModel
{
    public string Type { get; set; }
    public EnergyBreakdown Energy { get; set; }
    public List<MonthlyValue> MonthlyCosts { get; set; }

    public override IReadOnlyList<MonthlyValue> MonthlyBreakdown => MonthlyCosts;
}
