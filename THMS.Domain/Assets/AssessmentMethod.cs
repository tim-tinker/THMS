namespace THMS.Domain.Assets
{
    public enum AssessmentMethod
    {
        ApplyHomesteadCap,
        EqualMarketValue,
        AssessmentRatio,
        Manual
    }

    public static class AssessmentMethods
    {
        public const string ApplyHomesteadCap = "Apply homestead cap";
        public const string EqualMarketValue = "Equal to market value (100%)";
        public const string AssessmentRatio = "Assessment ratio";
        public const string Manual = "Manual";

        public static string Description(AssessmentMethod method) => method switch
        {
            AssessmentMethod.ApplyHomesteadCap => ApplyHomesteadCap,
            AssessmentMethod.EqualMarketValue => EqualMarketValue,
            AssessmentMethod.AssessmentRatio => AssessmentRatio,
            AssessmentMethod.Manual => Manual,
            _ => method.ToString()
        };

        public static AssessmentMethod Parse(string? text) => text switch
        {
            ApplyHomesteadCap => AssessmentMethod.ApplyHomesteadCap,
            EqualMarketValue => AssessmentMethod.EqualMarketValue,
            AssessmentRatio => AssessmentMethod.AssessmentRatio,
            Manual => AssessmentMethod.Manual,
            _ when Enum.TryParse<AssessmentMethod>(text, true, out var parsed) => parsed,
            _ => AssessmentMethod.ApplyHomesteadCap
        };
    }
}

