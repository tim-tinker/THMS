using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Finance
{
    /// <summary>
    /// Represents the commercial EV charging cost summary for a given period.
    /// Typically generated monthly, but can represent any date range.
    /// </summary>
    public class CommercialChargeCostSummary
    {
        /// <summary>
        /// Beginning of the reporting period.
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// End of the reporting period.
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// Total commercial charging energy delivered (in kWh)
        /// during the reporting period.
        /// </summary>
        public decimal TotalKwh { get; set; }

        /// <summary>
        /// Total commercial charging cost during the reporting period.
        /// </summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// Average cost per kWh of commercial charging.
        /// Computed as TotalCost / TotalKwh when TotalKwh > 0; otherwise 0.
        /// </summary>
        public decimal AverageCostPerKwh =>
            TotalKwh > 0 ? TotalCost / TotalKwh : 0;
    }
}
