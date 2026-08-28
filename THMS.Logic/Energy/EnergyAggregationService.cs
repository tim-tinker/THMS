using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Logic.Energy
{
    /// <summary>
    /// General-purpose energy aggregation service used by the Energy Dashboard.
    /// Produces daily, weekly, monthly, yearly, and custom-range aggregates.
    /// Uses the existing EnergyAggregationEngine for monthly EV+cost summaries.
    /// </summary>
    public class EnergyAggregationService
    {
        private readonly IEnergyDataStore _store;

        public EnergyAggregationService()
        {
            _store = new DataStoreFactory().GetEnergyStore();
        }

        // ---------------------------------------------------------
        // SUMMARY (always visible)
        // ---------------------------------------------------------
        public EnergySummary GetPeriodSummary(EnergyPeriod period)
        {
            return new EnergySummary
            {
                ProducedKwh = period.SolarKwh,
                ConsumedKwh = period.HomeConsumptionKwh,
                GridImportKwh = period.GridImportKwh,
                GridExportKwh = period.GridExportKwh,
                BatteryChargeKwh = period.BatteryChargeKwh,
                BatteryDischargeKwh = period.BatteryDischargeKwh,
                EvChargeKwh = period.EvChargeKwh,
            };
        }

        // ---------------------------------------------------------
        // DAILY
        // ---------------------------------------------------------
        public EnergyPeriod GetDay(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);
            return AggregatePeriod(start, end);
        }

        private List<EnergyIntervalRecord> AggregateHalfHourly(
            IEnumerable<SolarProductionInterval> intervals,
            IEnumerable<HomeCircuitAttribution> evAttr)
        {
            var grouped = intervals
                .GroupBy(i => TimeBucket.GetHalfHour(i.Timestamp))
                .OrderBy(g => g.Key);

            var evGrouped = evAttr
                .GroupBy(a => TimeBucket.GetHalfHour(a.Timestamp))
                .ToDictionary(g => g.Key, g => g.ToList());

            var list = new List<EnergyIntervalRecord>();

            foreach (var g in grouped)
            {
                var bucket = g.Key;

                decimal solar = g.Sum(x => x.EnergyProducedWh) / 1000m;
                decimal consumed = g.Sum(x => x.EnergyConsumedWh) / 1000m;
                decimal gridIn = g.Sum(x => x.ImportedFromGridWh) / 1000m;
                decimal gridOut = g.Sum(x => x.ExportedToGridWh) / 1000m;
                decimal battCharge = g.Sum(x => x.StoredInBatteriesWh) / 1000m;
                decimal battDischarge = g.Sum(x => x.DischargedFromBatteriesWh) / 1000m;

                decimal ev = 0;
                if (evGrouped.TryGetValue(bucket, out var evList))
                    ev = evList.Sum(x => x.TotalWh) / 1000m;

                list.Add(new EnergyIntervalRecord
                {
                    Timestamp = bucket,
                    SolarKwh = solar,
                    HomeConsumptionKwh = consumed,
                    GridImportKwh = gridIn,
                    GridExportKwh = gridOut,
                    BatteryChargeKwh = battCharge,
                    BatteryDischargeKwh = battDischarge,
                    EvChargeKwh = ev
                });
            }

            return list;
        }

        // ---------------------------------------------------------
        // WEEK / MONTH / YEAR / CUSTOM
        // ---------------------------------------------------------
        public EnergyPeriod GetWeek(DateTime date)
        {
            var start = date.Date.AddDays(-(int)date.DayOfWeek);
            var end = start.AddDays(7);
            return AggregatePeriod(start, end);
        }

        public EnergyPeriod GetMonth(DateTime date)
        {
            var start = new DateTime(date.Year, date.Month, 1);
            var end = start.AddMonths(1);
            return AggregatePeriod(start, end);
        }

        public EnergyPeriod GetYear(DateTime date)
        {
            var start = new DateTime(date.Year, 1, 1);
            var end = start.AddYears(1);
            return AggregatePeriod(start, end);
        }

        public EnergyPeriod GetRange(DateTime start, DateTime end)
        {
            return AggregatePeriod(start, end);
        }

        private EnergyPeriod AggregatePeriod(DateTime start, DateTime end)
        {
            // Period bounds are [start, end). Inclusive end would pull in the next
            // day's midnight as a second "day" (GetDay/Week/Month/Year all use exclusive end).
            var intervals = _store.GetSolarProductionIntervals(start, end)
                .Where(i => i.Timestamp >= start && i.Timestamp < end);
            var evAttr = _store.GetHomeCircuitAttribution(start, end)
                .Where(a => a.Timestamp >= start && a.Timestamp < end);

            var groupedByDay = intervals
                .GroupBy(i => i.Timestamp.Date)
                .OrderBy(g => g.Key);

            var evGroupedByDay = evAttr
                .GroupBy(a => a.Timestamp.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var records = new List<EnergyPeriodRecord>();

            foreach (var g in groupedByDay)
            {
                var day = g.Key;

                // Raw vendor intervals for this day
                var dayVendorIntervals = g.ToList();

                // Raw EV attribution for this day
                var dayEvAttr = evGroupedByDay.TryGetValue(day, out var evList)
                    ? evList
                    : Enumerable.Empty<HomeCircuitAttribution>();

                // Use your existing half-hour aggregator
                var halfHourIntervals = AggregateHalfHourly(dayVendorIntervals, dayEvAttr);

                // Compute daily totals from half-hour intervals
                var record = new EnergyPeriodRecord
                {
                    Date = day,
                    Intervals = halfHourIntervals,

                    SolarKwh = halfHourIntervals.Sum(h => h.SolarKwh),
                    HomeConsumptionKwh = halfHourIntervals.Sum(h => h.HomeConsumptionKwh),
                    GridImportKwh = halfHourIntervals.Sum(h => h.GridImportKwh),
                    GridExportKwh = halfHourIntervals.Sum(h => h.GridExportKwh),
                    BatteryChargeKwh = halfHourIntervals.Sum(h => h.BatteryChargeKwh),
                    BatteryDischargeKwh = halfHourIntervals.Sum(h => h.BatteryDischargeKwh),
                    EvChargeKwh = halfHourIntervals.Sum(h => h.EvChargeKwh)
                };

                ComputeDailyAnalytics(record, halfHourIntervals);
                records.Add(record);
            }

            var period = new EnergyPeriod
            {
                Start = start,
                End = end,
                Records = records,

                SolarKwh = records.Sum(r => r.SolarKwh),
                BatteryChargeKwh = records.Sum(r => r.BatteryChargeKwh),
                BatteryDischargeKwh = records.Sum(r => r.BatteryDischargeKwh),
                GridImportKwh = records.Sum(r => r.GridImportKwh),
                GridExportKwh = records.Sum(r => r.GridExportKwh),
                HomeConsumptionKwh = records.Sum(r => r.HomeConsumptionKwh),
                EvChargeKwh = records.Sum(r => r.EvChargeKwh)
            };

            ComputePeriodAnalytics(period);
            
            return period;
        }

        private void ComputeDailyAnalytics(EnergyPeriodRecord record, List<EnergyIntervalRecord> intervals)
        {
            if (intervals.Count == 0)
                return;

            // ---------------------------------------------------------
            // 1. Aggregate half-hour intervals into hourly buckets
            // ---------------------------------------------------------
            var hourly = intervals
                .GroupBy(i => new DateTime(i.Timestamp.Year, i.Timestamp.Month, i.Timestamp.Day, i.Timestamp.Hour, 0, 0))
                .Select(g => new
                {
                    Hour = g.Key,
                    SolarKwh = g.Sum(x => x.SolarKwh),
                    HomeKwh = g.Sum(x => x.HomeConsumptionKwh),
                    GridImportKwh = g.Sum(x => x.GridImportKwh),
                    GridExportKwh = g.Sum(x => x.GridExportKwh),
                    BatteryChargeKwh = g.Sum(x => x.BatteryChargeKwh),
                    BatteryDischargeKwh = g.Sum(x => x.BatteryDischargeKwh),
                    EvChargeKwh = g.Sum(x => x.EvChargeKwh)
                })
                .OrderBy(h => h.Hour)
                .ToList();

            // ---------------------------------------------------------
            // 2. Basic statistics (hourly)
            // ---------------------------------------------------------
            var hourlyValues = hourly.Select(h => h.HomeKwh).ToList(); // choose HomeConsumption as the "intensity" metric

            record.HourlyMin = hourlyValues.Min();
            record.HourlyMax = hourlyValues.Max();
            record.HourlyAvg = hourlyValues.Average();

            // Standard deviation
            var avg = record.HourlyAvg;
            record.HourlyStdDev = (decimal)Math.Sqrt(hourlyValues
                .Select(v => Math.Pow((double)(v - avg), 2))
                .Average());

            // ---------------------------------------------------------
            // 3. Time of Max (hourly)
            // ---------------------------------------------------------
            var maxHour = hourly.OrderByDescending(h => h.HomeKwh).First();
            record.TimeOfMax = maxHour.Hour;

            // ---------------------------------------------------------
            // 4. Peak Window (hourly)
            //    Find the contiguous 2-hour window with highest average load
            // ---------------------------------------------------------
            if (hourly.Count >= 2)
            {
                decimal bestAvg = -1;
                TimeSpan? bestStart = null;

                for (int i = 0; i < hourly.Count - 1; i++)
                {
                    var windowAvg = (hourly[i].HomeKwh + hourly[i + 1].HomeKwh) / 2m;
                    if (windowAvg > bestAvg)
                    {
                        bestAvg = windowAvg;
                        bestStart = hourly[i].Hour.TimeOfDay;
                    }
                }

                record.PeakWindowStart = bestStart;
                record.PeakWindowEnd = bestStart?.Add(TimeSpan.FromHours(2));
            }

            // ---------------------------------------------------------
            // 5. Solar Peak (kW)
            //    Convert hourly kWh to kW: kW = kWh / 1 hour
            // ---------------------------------------------------------
            var solarPeak = hourly.OrderByDescending(h => h.SolarKwh).First();
            record.SolarPeakKw = solarPeak.SolarKwh; // already hourly kWh → kW
            record.SolarPeakTime = solarPeak.Hour;

            // ---------------------------------------------------------
            // 6. Battery charge/discharge durations
            // ---------------------------------------------------------
            // Each half-hour interval with >0 charge/discharge counts as 30 minutes
            var chargeIntervals = intervals.Count(i => i.BatteryChargeKwh > 0);
            var dischargeIntervals = intervals.Count(i => i.BatteryDischargeKwh > 0);

            record.DailyBatteryChargeDuration = TimeSpan.FromMinutes(chargeIntervals * 30);
            record.DailyBatteryDischargeDuration = TimeSpan.FromMinutes(dischargeIntervals * 30);

            // ---------------------------------------------------------
            // 7. EV attribution breakdown (daily)
            // ---------------------------------------------------------
            // These are already aggregated in the record totals,
            // but we compute attribution breakdown from intervals.
            record.EvSolarKwh = intervals.Sum(i => i.EvChargeKwh * (i.SolarKwh > 0 ? 1 : 0));
            record.EvGridKwh = intervals.Sum(i => i.EvChargeKwh * (i.GridImportKwh > 0 ? 1 : 0));
            record.EvBatteryKwh = intervals.Sum(i => i.EvChargeKwh * (i.BatteryDischargeKwh > 0 ? 1 : 0));
        }

        private void ComputePeriodAnalytics(EnergyPeriod period)
        {
            if (period.Records.Count == 0)
                return;

            // ---------------------------------------------------------
            // 1. Basic statistics across all days
            // ---------------------------------------------------------
            var dailyMaxValues = period.Records.Select(r => r.HourlyMax).ToList();
            var dailyMinValues = period.Records.Select(r => r.HourlyMin).ToList();
            var dailyAvgValues = period.Records.Select(r => r.HourlyAvg).ToList();

            period.PeriodMin = dailyMinValues.Min();
            period.PeriodMax = dailyMaxValues.Max();
            period.PeriodAvg = dailyAvgValues.Average();

            // Standard deviation across daily averages
            var avg = period.PeriodAvg;
            period.PeriodStdDev = (decimal)Math.Sqrt(
                dailyAvgValues
                    .Select(v => Math.Pow((double)(v - avg), 2))
                    .Average()
            );

            // ---------------------------------------------------------
            // 2. Time-of-max across the entire period
            // ---------------------------------------------------------
            var maxRecord = period.Records
                .OrderByDescending(r => r.HourlyMax)
                .FirstOrDefault();

            period.PeriodTimeOfMax = maxRecord?.TimeOfMax;

            // ---------------------------------------------------------
            // 3. Peak window across the entire period
            //    Choose the day whose peak window had the highest load
            // ---------------------------------------------------------
            var bestPeakWindowRecord = period.Records
                .Where(r => r.PeakWindowStart.HasValue)
                .OrderByDescending(r => r.HourlyMax)
                .FirstOrDefault();

            if (bestPeakWindowRecord != null)
            {
                period.PeriodPeakWindowStart = bestPeakWindowRecord.PeakWindowStart;
                period.PeriodPeakWindowEnd = bestPeakWindowRecord.PeakWindowEnd;
            }

            // ---------------------------------------------------------
            // 4. Solar peak across the entire period
            // ---------------------------------------------------------
            var solarPeakRecord = period.Records
                .OrderByDescending(r => r.SolarPeakKw)
                .FirstOrDefault();

            if (solarPeakRecord != null)
            {
                period.PeriodSolarPeakKw = solarPeakRecord.SolarPeakKw;
                period.PeriodSolarPeakTime = solarPeakRecord.SolarPeakTime;
            }

            // ---------------------------------------------------------
            // 5. Battery contribution to home load
            // ---------------------------------------------------------
            if (period.HomeConsumptionKwh > 0)
            {
                period.BatteryContributionToHomeLoad =
                    period.BatteryDischargeKwh / period.HomeConsumptionKwh;
            }
            else
            {
                period.BatteryContributionToHomeLoad = 0;
            }

            // ---------------------------------------------------------
            // 6. EV attribution totals across the period
            // ---------------------------------------------------------
            period.EvSolarKwhTotal = period.Records.Sum(r => r.EvSolarKwh);
            period.EvGridKwhTotal = period.Records.Sum(r => r.EvGridKwh);
            period.EvBatteryKwhTotal = period.Records.Sum(r => r.EvBatteryKwh);
        }

    }
}
