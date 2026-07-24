using System.Collections.Generic;
using System.Linq;

namespace THMS.Domain
{
    public class MonthlyValue
    {
        public string Month { get; set; }
        public decimal Amount { get; set; }

        /// <summary>
        /// Standardized list of month names used across all THMS modules.
        /// </summary>
        public static readonly string[] MonthNames =
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        /// <summary>
        /// Generates a 12‑month list with the same amount for each month.
        /// </summary>
        public static List<MonthlyValue> Generate12Months(decimal monthlyAmount)
        {
            return MonthNames
                .Select(m => new MonthlyValue
                {
                    Month = m,
                    Amount = monthlyAmount
                })
                .ToList();
        }
    }
}
