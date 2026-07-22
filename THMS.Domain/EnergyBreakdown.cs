namespace THMS.Domain;

public class EnergyBreakdown
{
    public EnergyBreakdown(int v1, int v2, int v3)
    {
        HomeCharging = v1;
        PublicCharging = v2;
        Regen = v3;
    }

    public double HomeCharging { get; set; }
    public double PublicCharging { get; set; }
    public double Regen { get; set; }
}
