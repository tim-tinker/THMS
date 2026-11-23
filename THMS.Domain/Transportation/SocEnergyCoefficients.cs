namespace THMS.Domain.Transportation
{
    /// <summary>
    /// Stored battery energy as E(s) = A1·s + A2·s² + A3·s³,
    /// where s is displayed SOC in percent.
    /// </summary>
    public readonly record struct SocEnergyCoefficients(decimal A1, decimal A2, decimal A3);
}
