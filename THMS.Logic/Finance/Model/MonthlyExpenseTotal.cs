using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Logic.Finance.Model
{
    public class MonthlyExpenseTotal
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
    }

}
