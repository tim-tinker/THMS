using System;
using System.Collections.Generic;
using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class EnergyPeriodViewModel
    {
        // ---------------------------------------------------------
        // Period identity
        // ---------------------------------------------------------
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;

        // ---------------------------------------------------------
        // Daily records (one per day). Half-hour points live on
        // <see cref="EnergyPeriodRecordViewModel.Intervals"/>.
        // ---------------------------------------------------------
        public List<EnergyPeriodRecordViewModel> Records { get; set; } = new();

        /// <summary>All half-hour intervals in the period, ordered by timestamp.</summary>
        public IReadOnlyList<EnergyIntervalRecord> Intervals =>
            Records.SelectMany(r => r.Intervals).OrderBy(i => i.Timestamp).ToList();

        // ---------------------------------------------------------
        // Period totals (existing)
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

        // Basic statistics across all days
        public decimal PeriodMin { get; set; }
        public decimal PeriodMax { get; set; }
        public decimal PeriodAvg { get; set; }
        public decimal PeriodStdDev { get; set; }

        // Time-of-max across the entire period
        public DateTime? PeriodTimeOfMax { get; set; }

        // Peak window across the entire period
        public TimeSpan? PeriodPeakWindowStart { get; set; }
        public TimeSpan? PeriodPeakWindowEnd { get; set; }

        // Solar peak across the entire period
        public decimal PeriodSolarPeakKw { get; set; }
        public DateTime? PeriodSolarPeakTime { get; set; }

        // Battery contribution to home load
        public decimal BatteryContributionToHomeLoad { get; set; }

        // EV attribution totals across the period
        public decimal EvSolarKwhTotal { get; set; }
        public decimal EvGridKwhTotal { get; set; }
        public decimal EvBatteryKwhTotal { get; set; }

        // ---------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------
        public EnergyPeriodViewModel(EnergyPeriod period)
        {
            Start = period.Start;
            End = period.End;

            Records = (from record in period.Records select new EnergyPeriodRecordViewModel(record)).ToList();

            SolarKwh = period.SolarKwh;
            BatteryChargeKwh = period.BatteryChargeKwh;
            BatteryDischargeKwh = period.BatteryDischargeKwh;
            GridImportKwh = period.GridImportKwh;
            GridExportKwh = period.GridExportKwh;
            HomeConsumptionKwh = period.HomeConsumptionKwh;
            EvChargeKwh = period.EvChargeKwh;

            PeriodMin = period.PeriodMin;
            PeriodMax = period.PeriodMax;
            PeriodAvg = period.PeriodAvg;
            PeriodStdDev = period.PeriodStdDev;
            PeriodTimeOfMax = period.PeriodTimeOfMax;
            PeriodPeakWindowStart = period.PeriodPeakWindowStart;
            PeriodPeakWindowEnd = period.PeriodPeakWindowEnd;
            PeriodSolarPeakKw = period.PeriodSolarPeakKw;
            PeriodSolarPeakTime = period.PeriodSolarPeakTime;
            BatteryContributionToHomeLoad = period.BatteryContributionToHomeLoad;
            EvSolarKwhTotal = period.EvSolarKwhTotal;
            EvGridKwhTotal = period.EvGridKwhTotal;
            EvBatteryKwhTotal = period.EvBatteryKwhTotal;
        }

        /// <summary>Min/max/avg/stddev of a daily metric across <see cref="Records"/>.</summary>
        public (decimal Min, decimal Max, decimal Avg, decimal StdDev) GetDailyStats(
            Func<EnergyPeriodRecordViewModel, decimal> selector)
        {
            if (Records.Count == 0)
                return (0, 0, 0, 0);

            var values = Records.Select(selector).ToList();
            var min = values.Min();
            var max = values.Max();
            var avg = values.Average();
            var stdDev = (decimal)Math.Sqrt(
                values.Select(v => Math.Pow((double)(v - avg), 2)).Average());

            return (min, max, avg, stdDev);
        }

        // Empty constructor for Custom before user selects a range
        public EnergyPeriodViewModel()
        {
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            Records = [];
        }
    }
}
