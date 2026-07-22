using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain
{
    public class FinanceAccount : BaseDomainModel
    {
        public decimal Balance { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public List<MonthlyValue> MonthlyNet { get; set; }

        public override IReadOnlyList<MonthlyValue> MonthlyBreakdown => MonthlyNet;
    }
}
