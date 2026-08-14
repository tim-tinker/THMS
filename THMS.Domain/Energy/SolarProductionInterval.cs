namespace THMS.Domain.Energy
{
    public class SolarProductionInterval : BaseDomainModel
    {
        /// <summary>
        /// Timestamp of the interval (vendor data is typically point-based or short-interval).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Energy produced by solar panels during the interval (Wh).
        /// </summary>
        public decimal EnergyProducedWh { get; set; }

        /// <summary>
        /// Energy consumed by the home during the interval (Wh).
        /// </summary>
        public decimal EnergyConsumedWh { get; set; }

        /// <summary>
        /// Energy exported to the grid (Wh).
        /// </summary>
        public decimal ExportedToGridWh { get; set; }

        /// <summary>
        /// Energy imported from the grid (Wh).
        /// </summary>
        public decimal ImportedFromGridWh { get; set; }

        /// <summary>
        /// Energy stored in batteries (Wh).
        /// </summary>
        public decimal StoredInBatteriesWh { get; set; }

        /// <summary>
        /// Energy discharged from batteries (Wh).
        /// </summary>
        public decimal DischargedFromBatteriesWh { get; set; }
    }
}
