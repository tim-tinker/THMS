namespace THMS.Domain.Finance
{
    public class UpdaterResult
    {
        public int AccountsUpdated { get; set; }
        public int TransactionsImported { get; set; }
        public int TransfersDetected { get; set; }
        public int RecurringRulesUpdated { get; set; }
        public bool ForecastUpdated { get; set; }
        public bool RollOffCompleted { get; set; }
    }
}
