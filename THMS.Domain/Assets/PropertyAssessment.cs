namespace THMS.Domain.Assets
{
    public static class PropertyAssessment
    {
        public static bool IsCalculated(AssessmentMethod method) =>
            method != AssessmentMethod.Manual;

        public static decimal TotalMarket(decimal landMarket, decimal improvementMarket) =>
            landMarket + improvementMarket;

        public static decimal AssessedValue(
            AssessmentMethod method,
            decimal totalMarket,
            decimal homesteadCap,
            decimal assessmentRatioPercent) => method switch
        {
            AssessmentMethod.ApplyHomesteadCap => NonNegative(totalMarket - homesteadCap),
            AssessmentMethod.EqualMarketValue => NonNegative(totalMarket),
            AssessmentMethod.AssessmentRatio => decimal.Round(
                NonNegative(totalMarket) * assessmentRatioPercent / 100m,
                2,
                MidpointRounding.AwayFromZero),
            _ => 0m
        };

        private static decimal NonNegative(decimal value) => value < 0m ? 0m : value;
    }
}
