using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class EnergyPeriodRecord
    {
        public DateTime Date { get; set; }
        public List<EnergyIntervalRecord> Intervals { get; set; } = new();

        public decimal SolarKwh { get; set; }
        public decimal BatteryChargeKwh { get; set; }
        public decimal BatteryDischargeKwh { get; set; }
        public decimal GridImportKwh { get; set; }
        public decimal GridExportKwh { get; set; }
        public decimal EvChargeKwh { get; set; }
        public decimal HomeConsumptionKwh { get; set; }

        // ---------------------------------------------------------
        // NEW: Daily analytical metrics (based on hourly aggregates)
        // ---------------------------------------------------------

        // Basic statistics
        public decimal HourlyMin { get; set; }
        public decimal HourlyMax { get; set; }
        public decimal HourlyAvg { get; set; }
        public decimal HourlyStdDev { get; set; }

        // Temporal characteristics
        public DateTime? TimeOfMax { get; set; }   // timestamp of hourly max
        public TimeSpan? PeakWindowStart { get; set; }
        public TimeSpan? PeakWindowEnd { get; set; }

        // Solar-specific daily metrics
        public decimal SolarPeakKw { get; set; }
        public DateTime? SolarPeakTime { get; set; }

        // Battery-specific daily metrics
        public TimeSpan DailyBatteryChargeDuration { get; set; }
        public TimeSpan DailyBatteryDischargeDuration { get; set; }

        // EV attribution (daily)
        public decimal EvSolarKwh { get; set; }
        public decimal EvGridKwh { get; set; }
        public decimal EvBatteryKwh { get; set; }
    }
}
