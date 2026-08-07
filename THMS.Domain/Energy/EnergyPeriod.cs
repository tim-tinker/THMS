using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class EnergyPeriod
    {
        // ---------------------------------------------------------
        // Metadata
        // ---------------------------------------------------------
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        // ---------------------------------------------------------
        // Time-series data (daily or monthly)
        // ---------------------------------------------------------
        public List<EnergyPeriodRecord> Records { get; set; }

        // ---------------------------------------------------------
        // Totals for the entire period
        // ---------------------------------------------------------
        public decimal SolarKwh { get; set; }
        public decimal BatteryChargeKwh { get; set; }
        public decimal BatteryDischargeKwh { get; set; }
        public decimal GridImportKwh { get; set; }
        public decimal GridExportKwh { get; set; }
        public decimal HomeConsumptionKwh { get; set; }
        public decimal EvChargingKwh { get; set; }
    }
}
