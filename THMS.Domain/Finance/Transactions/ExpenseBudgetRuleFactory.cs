namespace THMS.Domain.Finance.Transactions
{
    public class ExpenseBudgetRuleFactory
    {
        public ExpenseBudgetRule Create(string category)
        {
            return category switch
            {
                "Utilities" => new UtilityBudgetRule(),

                // Future rule types:
                //"Groceries" => new GroceriesBudgetRule(),
                //"Medical" => new MedicalBudgetRule(),
                //"AutoFuel" => new AutoFuelBudgetRule(),

                _ => throw new InvalidOperationException(
                    $"Unknown ExpenseBudgetRule category: {category}")
            };
        }
    }
}
