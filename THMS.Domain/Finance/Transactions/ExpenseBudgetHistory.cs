namespace THMS.Domain.Finance.Transactions
{
    public class ExpenseBudgetHistory : BaseDomainModel
    {
        public Guid BudgetRuleId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal StartingBalance { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal ActualExpenses { get; set; }
        public decimal Remaining { get; set; }
        public decimal EndingBalance { get; set; }
        public decimal RecommendedAmount { get; set; }
        public bool IsClosed { get; set; }

        public void RecalculateRemaining()
        {
            Remaining = StartingBalance + Math.Abs(BudgetAmount) - ActualExpenses;
            EndingBalance = Remaining;
        }
    }
}
