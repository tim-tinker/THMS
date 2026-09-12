namespace THMS.Logic.ViewModels.Finance
{
    public class CashFlowForecast
    {
        public decimal CurrentBalance { get; set; }
        public decimal ForecastedBalanceNextPayday { get; set; }
        public decimal ForecastedBalanceTwoPaydays { get; set; }
        public decimal ForecastedBalanceAfterPlanned { get; set; }
        public DateTime? NextPayday { get; set; }
        public DateTime? SecondPayday { get; set; }
    }
}
