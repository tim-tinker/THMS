namespace THMS.Domain.Energy
{
    /// <summary>
    /// Represents aggregated energy and cost data for a single month.
    /// </summary>
    public class MonthlyEnergySummary
    {
        public int Year { get; set; }
        public int Month { get; set; }

        // Energy totals (Wh)
        public decimal SolarProducedWh { get; set; }
        public decimal SolarConsumedWh { get; set; }
        public decimal GridImportedWh { get; set; }
        public decimal GridExportedWh { get; set; }
        public decimal BatteryStoredWh { get; set; }
        public decimal BatteryDischargedWh { get; set; }

        // EV charging totals (Wh)
        public decimal EvChargingWh { get; set; }
        public decimal EvChargingSolarWh { get; set; }
        public decimal EvChargingBatteryWh { get; set; }
        public decimal EvChargingGridWh { get; set; }

        // Cost totals ($)
        public decimal SolarAvoidedCost { get; set; }
        public decimal BatteryValue { get; set; }
        public decimal GridCost { get; set; }
        public decimal CommercialChargingCost { get; set; }

        // Flags
        public bool IsPartial { get; set; }
    }
}
