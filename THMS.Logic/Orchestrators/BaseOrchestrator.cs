namespace THMS.Logic.Orchestrators
{
    public class BaseOrchestrator
    {
        protected DateTime GetStartDate(DateTime end, string period)
        {
            DateTime start = end.AddMonths(-1);
            switch (period)
            {
                case "Year":
                    start = end.AddYears(-1);
                    break;

                case "Lifetime":
                    start = DateTime.MinValue;
                    break;

                default:
                    start = end.AddMonths(-1);
                    break;
            }

            return start;
        }
    }
}
