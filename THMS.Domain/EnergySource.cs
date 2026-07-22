using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain
{
    public class EnergySource : BaseDomainModel
    {
        public decimal MonthlyKwh { get; set; }
        public decimal CostPerKwh { get; set; }
        public decimal MonthlyCost => MonthlyKwh * CostPerKwh;
        public List<MonthlyValue> MonthlyCosts { get; set; }

        public override IReadOnlyList<MonthlyValue> MonthlyBreakdown => MonthlyCosts;
    }
}
