namespace THMS.Domain.Finance.Transactions
{
    public static class BudgetFrequencyExtension
    {
        public static int SoonDays(this BudgetFrequency frequency) => frequency switch
        {
            BudgetFrequency.Weekly => 1,
            BudgetFrequency.Biweekly => 3,
            BudgetFrequency.Monthly => 7,
            BudgetFrequency.Quarterly => 18,
            BudgetFrequency.Annual => 30,
            _ => 7
        };
    }
}
