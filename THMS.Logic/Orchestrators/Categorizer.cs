using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class Categorizer
    {
        public void ApplyCategories(List<PostedTransaction> tx)
        {
            foreach (var t in tx)
            {
                if (t.Description.Contains("Amazon", StringComparison.OrdinalIgnoreCase))
                    t.Category = "Shopping";

                else if (t.Description.Contains("Walmart", StringComparison.OrdinalIgnoreCase))
                    t.Category = "Groceries";

                else if (t.Amount < 0 && t.Description.Contains("Payment"))
                    t.Category = "Payment";

                else
                    t.Category = "Uncategorized";
            }
        }
    }
}
