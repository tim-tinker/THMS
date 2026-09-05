namespace THMS.Domain.Finance.Transactions
{
    public class UtilityBudgetRule : ExpenseBudgetRule
    {
        public static readonly string[] UtilityCategories =
        {
            "Electric",
            "Water",
            "Gas",
            "Cell Phone",
        };

        public override IReadOnlyList<string> IncludedCategories => UtilityCategories;

        public UtilityBudgetRule()
        {
            Category = "Utilities";
        }
    }
}