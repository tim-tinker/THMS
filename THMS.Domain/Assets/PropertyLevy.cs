namespace THMS.Domain.Assets
{
    public static class PropertyLevy
    {
        public static bool IsCalculated(LevyMethod method) => method != LevyMethod.Manual;

        public static decimal TaxableValue(decimal assessedValue, decimal exemptions) =>
            assessedValue > exemptions ? assessedValue - exemptions : 0m;

        public static decimal Tax(LevyMethod method, decimal taxableValue, decimal rate)
        {
            var unit = method == LevyMethod.Millage ? 1000m : 100m;
            return decimal.Round(taxableValue * rate / unit, 2, MidpointRounding.AwayFromZero);
        }
    }
}
