using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Finance.Transactions
{
    public abstract class ExpenseBudgetRule : BaseDomainModel
    {
        public Guid AccountId { get; set; }
        public string Category { get; set; } = string.Empty;

        public int MonthsToAverage { get; set; } = 36;
        public ExpenseSmoothingMode SmoothingMode { get; set; } = ExpenseSmoothingMode.Hybrid;

        public decimal CurrentAverage { get; set; }
        public DateTime NextOccurrence { get; set; }

        public abstract IReadOnlyList<string> IncludedCategories { get; }
    }
}
