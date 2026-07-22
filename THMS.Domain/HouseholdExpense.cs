using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain
{
    public class HouseholdExpense : BaseDomainModel
    {
        public decimal MonthlyCost { get; set; }
        public string Category { get; set; }
        public string SharedWith { get; set; }
        public List<MonthlyValue> MonthlyBreakdownInternal { get; set; }

        public override IReadOnlyList<MonthlyValue> MonthlyBreakdown => MonthlyBreakdownInternal;
    }
}
