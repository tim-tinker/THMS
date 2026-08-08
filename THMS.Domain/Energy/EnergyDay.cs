using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class EnergyDay
    {
        // ---------------------------------------------------------
        // Basic metadata
        // ---------------------------------------------------------
        public DateTime Date { get; set; }

        // ---------------------------------------------------------
        // Hourly time-series data (for charts)
        // ---------------------------------------------------------
        public IReadOnlyList<EnergyIntervalRecord> Intervals { get; set; }

        // ---------------------------------------------------------
        // Battery SOC timeline (for SOC chart)
        // ---------------------------------------------------------
        public List<BatterySocRecord> BatterySocTimeline { get; set; }

        // ---------------------------------------------------------
        // Daily totals (for summary + breakdown)
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
