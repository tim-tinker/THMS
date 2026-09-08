namespace THMS.Domain.Finance.Transactions
{
    public sealed class CategoryUsage
    {
        public int TransactionCount { get; init; }
        public int RecurringRuleCount { get; init; }
        public int BudgetRuleCount { get; init; }
        public int LearnedMappingCount { get; init; }

        public int Total => TransactionCount + RecurringRuleCount + BudgetRuleCount + LearnedMappingCount;
    }
}
