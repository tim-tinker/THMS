using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly EnergyAggregationEngine _monthlyEngine;

        public EnergyAggregationService(IEnergyDataStore store)
        {
            _store = store;
            _monthlyEngine = new EnergyAggregationEngine(store);
        }

        // ---------------------------------------------------------
        // SUMMARY (always visible)
        // ---------------------------------------------------------
        public EnergySummary GetSummary(DateTime date)
        {
            var day = GetDay(date);

            return new EnergySummary
            {
                ProducedKwh = day.SolarKwh,
                ConsumedKwh = day.HomeConsumptionKwh,
                GridImportKwh = day.GridImportKwh,
                GridExportKwh = day.GridExportKwh,
                BatteryNetKwh = day.BatteryDischargeKwh - day.BatteryChargeKwh,
                NetImportKwh = day.GridImportKwh - day.GridExportKwh,
                EvChargingKwh = day.EvChargingKwh
            };
        }

        // ---------------------------------------------------------
        // DAILY
        // ---------------------------------------------------------
        public EnergyDay GetDay(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var intervals = _store.GetSolarVendorIntervals(start, end);
            var evAttr = _store.GetEvAttribution(start, end);
            var soc = _store.GetBatterySocTimeline(start, end);

            var intervalsHalfHour = AggregateHalfHourly(intervals, evAttr);

            return new EnergyDay
            {
                Date = date,
                Intervals = intervalsHalfHour,
                BatterySocTimeline = [.. soc],

                SolarKwh = intervalsHalfHour.Sum(h => h.SolarKwh),
                BatteryChargeKwh = intervalsHalfHour.Sum(h => h.BatteryChargeKwh),
                BatteryDischargeKwh = intervalsHalfHour.Sum(h => h.BatteryDischargeKwh),
                GridImportKwh = intervalsHalfHour.Sum(h => h.GridImportKwh),
                GridExportKwh = intervalsHalfHour.Sum(h => h.GridExportKwh),
                HomeConsumptionKwh = intervalsHalfHour.Sum(h => h.HomeConsumptionKwh),
                EvChargingKwh = intervalsHalfHour.Sum(h => h.EvChargingKwh)
            };
        }

        private List<EnergyIntervalRecord> AggregateHalfHourly(
            IEnumerable<SolarVendorInterval> intervals,
            IEnumerable<EnergyAttributionResult> evAttr)
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
                    ev = evList.Sum(x => x.EvChargeWh) / 1000m;

                list.Add(new EnergyIntervalRecord
                {
                    Timestamp = bucket,
                    SolarKwh = solar,
                    HomeConsumptionKwh = consumed,
                    GridImportKwh = gridIn,
                    GridExportKwh = gridOut,
                    BatteryChargeKwh = battCharge,
                    BatteryDischargeKwh = battDischarge,
                    EvChargingKwh = ev
                });
            }

            return list;
        }

        private DateTime GetHalfHourBucket(DateTime timestamp)
        {
            var minute = timestamp.Minute;
            var bucketMinute = minute < 15 ? 0 :
                               minute < 45 ? 30 : 0;

            var bucketHour = minute < 45 ? timestamp.Hour : (timestamp.Hour + 1) % 24;
            return new DateTime(
                timestamp.Year,
                timestamp.Month,
                timestamp.Day,
                bucketHour,
                bucketMinute,
                0);
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
            var intervals = _store.GetSolarVendorIntervals(start, end);
            var evAttr = _store.GetEvAttribution(start, end);

            var grouped = intervals
                .GroupBy(i => i.Timestamp.Date)
                .OrderBy(g => g.Key);

            var evGrouped = evAttr
                .GroupBy(a => a.Timestamp.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var records = new List<EnergyPeriodRecord>();

            foreach (var g in grouped)
            {
                var day = g.Key;

                decimal solar = g.Sum(x => x.EnergyProducedWh) / 1000m;
                decimal consumed = g.Sum(x => x.EnergyConsumedWh) / 1000m;
                decimal gridIn = g.Sum(x => x.ImportedFromGridWh) / 1000m;
                decimal gridOut = g.Sum(x => x.ExportedToGridWh) / 1000m;
                decimal battCharge = g.Sum(x => x.StoredInBatteriesWh) / 1000m;
                decimal battDischarge = g.Sum(x => x.DischargedFromBatteriesWh) / 1000m;

                decimal ev = 0;
                if (evGrouped.TryGetValue(day, out var evList))
                    ev = evList.Sum(x => x.EvChargeWh) / 1000m;

                records.Add(new EnergyPeriodRecord
                {
                    Date = day,
                    SolarKwh = solar,
                    HomeConsumptionKwh = consumed,
                    GridImportKwh = gridIn,
                    GridExportKwh = gridOut,
                    BatteryChargeKwh = battCharge,
                    BatteryDischargeKwh = battDischarge,
                    EvChargingKwh = ev
                });
            }

            return new EnergyPeriod
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
                EvChargingKwh = records.Sum(r => r.EvChargingKwh)
            };
        }
    }
}
