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

        public static (DateTime Start, DateTime End) GetHistoryRange(string period)
        {
            var today = DateTime.Today;
            return (GetStartDate(today, period), today.AddDays(1).AddTicks(-1));
        }
    }
}
