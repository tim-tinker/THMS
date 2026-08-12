using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class EnergyPeriodRecordViewModel
    {
        public DateTime Date { get; set; }

        /// <summary>Half-hour points for this day (used by the day chart).</summary>
        public List<EnergyIntervalRecord> Intervals { get; set; } = new();

        // Daily totals
        public decimal SolarKwh { get; set; }
        public decimal HomeConsumptionKwh { get; set; }
        public decimal GridImportKwh { get; set; }
        public decimal GridExportKwh { get; set; }
        public decimal BatteryChargeKwh { get; set; }
        public decimal BatteryDischargeKwh { get; set; }
        public decimal EvChargeKwh { get; set; }

        // Daily analytics
        public decimal HourlyMin { get; set; }
        public decimal HourlyMax { get; set; }
        public decimal HourlyAvg { get; set; }
        public decimal HourlyStdDev { get; set; }

        public DateTime? TimeOfMax { get; set; }
        public TimeSpan? PeakWindowStart { get; set; }
        public TimeSpan? PeakWindowEnd { get; set; }

        public decimal SolarPeakKw { get; set; }
        public DateTime? SolarPeakTime { get; set; }

        public TimeSpan DailyBatteryChargeDuration { get; set; }
        public TimeSpan DailyBatteryDischargeDuration { get; set; }

        public decimal EvSolarKwh { get; set; }
        public decimal EvGridKwh { get; set; }
        public decimal EvBatteryKwh { get; set; }

        public EnergyPeriodRecordViewModel(EnergyPeriodRecord r)
        {
            Date = r.Date;
            Intervals = r.Intervals.ToList();

            // Daily totals
            SolarKwh = r.SolarKwh;
            HomeConsumptionKwh = r.HomeConsumptionKwh;
            GridImportKwh = r.GridImportKwh;
            GridExportKwh = r.GridExportKwh;
            BatteryChargeKwh = r.BatteryChargeKwh;
            BatteryDischargeKwh = r.BatteryDischargeKwh;
            EvChargeKwh = r.EvChargeKwh;

            // Daily analytics
            HourlyMin = r.HourlyMin;
            HourlyMax = r.HourlyMax;
            HourlyAvg = r.HourlyAvg;
            HourlyStdDev = r.HourlyStdDev;

            TimeOfMax = r.TimeOfMax;
            PeakWindowStart = r.PeakWindowStart;
            PeakWindowEnd = r.PeakWindowEnd;

            SolarPeakKw = r.SolarPeakKw;
            SolarPeakTime = r.SolarPeakTime;

            DailyBatteryChargeDuration = r.DailyBatteryChargeDuration;
            DailyBatteryDischargeDuration = r.DailyBatteryDischargeDuration;

            EvSolarKwh = r.EvSolarKwh;
            EvGridKwh = r.EvGridKwh;
            EvBatteryKwh = r.EvBatteryKwh;
        }
    }
}
