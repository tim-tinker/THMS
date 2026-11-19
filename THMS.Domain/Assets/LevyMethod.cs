namespace THMS.Domain.Assets
{
    public enum LevyMethod
    {
        RatePerHundred,
        Millage,
        Manual
    }

    public static class LevyMethods
    {
        public const string RatePerHundred = "Rate per $100";
        public const string Millage = "Millage (per $1,000)";
        public const string Manual = "Manual entry";

        public static string Description(LevyMethod method) => method switch
        {
            LevyMethod.RatePerHundred => RatePerHundred,
            LevyMethod.Millage => Millage,
            LevyMethod.Manual => Manual,
            _ => method.ToString()
        };

        public static LevyMethod Parse(string? text) => text switch
        {
            RatePerHundred => LevyMethod.RatePerHundred,
            Millage => LevyMethod.Millage,
            Manual => LevyMethod.Manual,
            _ when Enum.TryParse<LevyMethod>(text, true, out var parsed) => parsed,
            _ => LevyMethod.RatePerHundred
        };
    }
}
