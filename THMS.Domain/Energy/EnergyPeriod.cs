using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class EnergyPeriod
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public List<EnergyPeriodRecord> Records { get; set; } = new();

        // ---------------------------------------------------------
        // Existing period totals
        // ---------------------------------------------------------
        public decimal SolarKwh { get; set; }
        public decimal HomeConsumptionKwh { get; set; }
        public decimal GridImportKwh { get; set; }
        public decimal GridExportKwh { get; set; }
        public decimal BatteryChargeKwh { get; set; }
        public decimal BatteryDischargeKwh { get; set; }
        public decimal EvChargeKwh { get; set; }

        // ---------------------------------------------------------
        // NEW: Period-level analytics
        // ---------------------------------------------------------

        // Basic statistics across all hourly values in the period
        public decimal PeriodMin { get; set; }
        public decimal PeriodMax { get; set; }
        public decimal PeriodAvg { get; set; }
        public decimal PeriodStdDev { get; set; }

        // Time-of-max across the entire period (date + hour)
        public DateTime? PeriodTimeOfMax { get; set; }

        // Peak window across the entire period (time-of-day)
        public TimeSpan? PeriodPeakWindowStart { get; set; }
        public TimeSpan? PeriodPeakWindowEnd { get; set; }

        // Solar peak across the entire period
        public decimal PeriodSolarPeakKw { get; set; }
        public DateTime? PeriodSolarPeakTime { get; set; }

        // Battery contribution to home load across the period
        public decimal BatteryContributionToHomeLoad { get; set; }

        // EV attribution totals across the period
        public decimal EvSolarKwhTotal { get; set; }
        public decimal EvGridKwhTotal { get; set; }
        public decimal EvBatteryKwhTotal { get; set; }
    }
}
