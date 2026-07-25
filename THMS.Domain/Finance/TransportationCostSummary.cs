using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Finance
{
    /// <summary>
    /// Represents the complete monthly transportation cost summary,
    /// including EV charging (home + commercial), gasoline, maintenance,
    /// insurance, registration, parking, and tolls.
    /// </summary>
    public class TransportationCostSummary
    {
        /// <summary>
        /// First day of the month.
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// Last day of the month.
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// Cost of home EV charging for the month.
        /// </summary>
        public decimal HomeEvChargingCost { get; set; }

        /// <summary>
        /// Cost of commercial EV charging for the month.
        /// </summary>
        public decimal CommercialEvChargingCost { get; set; }

        /// <summary>
        /// Cost of gasoline for ICE vehicles for the month.
        /// </summary>
        public decimal GasCost { get; set; }

        /// <summary>
        /// Cost of vehicle maintenance for the month.
        /// </summary>
        public decimal MaintenanceCost { get; set; }

        /// <summary>
        /// Cost of vehicle insurance for the month.
        /// </summary>
        public decimal InsuranceCost { get; set; }

        /// <summary>
        /// Registration fees allocated to this month.
        /// </summary>
        public decimal RegistrationCost { get; set; }

        /// <summary>
        /// Parking fees for the month.
        /// </summary>
        public decimal ParkingCost { get; set; }

        /// <summary>
        /// Toll charges for the month.
        /// </summary>
        public decimal TollCost { get; set; }

        /// <summary>
        /// Total transportation cost for the month.
        /// </summary>
        public decimal TotalCost =>
            HomeEvChargingCost +
            CommercialEvChargingCost +
            GasCost +
            MaintenanceCost +
            InsuranceCost +
            RegistrationCost +
            ParkingCost +
            TollCost;
    }
}
