using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Finance
{
    /// <summary>
    /// The monthly financial summary of EV charging performed at home, using 
    /// <see cref="HomeCircuitReading"/>, 
    /// <see cref="SolarProductionReading"/>, and 
    /// <see cref="ElectricUtilityBill"/>.
    /// </summary>
    public class HomeChargeCostSummary
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        /// <summary>
        /// How much grid energy did the EV consume this month?
        /// </summary>
        public decimal EvGridKwh { get; set; }

        /// <summary>
        /// How much did that grid energy cost?
        /// </summary>
        public decimal EvCost { get; set; }

        /// <summary>
        /// Average cost per kWh of home EV charging for this billing cycle.
        /// Computed as EvCost / EvGridKwh when EvGridKwh > 0; otherwise 0.
        /// </summary>
        public decimal AverageCostPerKwh =>
            EvGridKwh > 0 ? EvCost / EvGridKwh : 0;
    }
}
