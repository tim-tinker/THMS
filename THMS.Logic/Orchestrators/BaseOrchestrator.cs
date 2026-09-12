namespace THMS.Logic.Orchestrators
{
    public class BaseOrchestrator
    {
        public static DateTime GetStartDate(DateTime end, string period)
        {
            return period switch
            {
                "Year" => end.AddYears(-1),
                "Lifetime" => DateTime.MinValue,
                _ => end.AddMonths(-1)
            };
        }
    }
}
