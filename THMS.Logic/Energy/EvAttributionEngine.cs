using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Logic.Energy;

public class EvAttributionEngine
{
    private readonly IEnergyDataStore _store;
    private readonly List<EnergyAttributionResult> _results = [];

    public IEnumerable<EnergyAttributionResult> Results => _results;
    public int ResultCount => _results.Count;

    public EvAttributionEngine(IEnergyDataStore store)
    {
        _store = store;
    }

    public void Compute(DateTime start, DateTime end)
    {
        _results.Clear();

        // Raw data
        var solar = _store.GetSolarVendorIntervals(start, end);
        var ev = _store.GetEvCircuitReadings(start, end);

        // Half-hour buckets
        var buckets = HalfHourBucketJoin(solar, ev);

        foreach (var b in buckets)
        {
            decimal evKwh = b.EvChargingKwh;

            if (evKwh <= 0)
                continue;

            // Available sources
            decimal solarAvailable =
                b.SolarKwh
                - b.HomeConsumptionKwh
                - b.BatteryChargeKwh;

            if (solarAvailable < 0)
                solarAvailable = 0;

            decimal gridAvailable = b.GridImportKwh;

            // Attribution
            decimal gridToEv = Math.Min(evKwh, gridAvailable);
            decimal remaining = evKwh - gridToEv;

            decimal solarToEv = Math.Min(remaining, solarAvailable);
            remaining -= solarToEv;
            decimal batteryToEv = remaining;

            // Store result
            var result = new EnergyAttributionResult
            {
                Timestamp = b.Timestamp,
                EvChargeWh = evKwh * 1000m,
                SolarWh = solarToEv * 1000m,
                BatteryWh = batteryToEv * 1000m,
                GridWh = gridToEv * 1000m
            };

            _results.Add(result);
        }
    }

    private IEnumerable<HalfHourBucket> HalfHourBucketJoin(
        IEnumerable<SolarVendorInterval> solar,
        IEnumerable<EvCircuitReading> ev)
    {
        // Step 1: bucket solar intervals
        var solarBuckets = solar
            .GroupBy(i => TimeBucket.GetHalfHour(i.Timestamp))
            .ToDictionary(g => g.Key, g => AggregateSolar(g));

        // Step 2: bucket EV intervals
        var evBuckets = ev
            .GroupBy(i => TimeBucket.GetHalfHour(i.Timestamp))
            .ToDictionary(g => g.Key, g => AggregateEv(g));

        // Step 3: join buckets
        var keys = solarBuckets.Keys.Union(evBuckets.Keys).OrderBy(k => k);

        foreach (var key in keys)
        {
            solarBuckets.TryGetValue(key, out var s);
            evBuckets.TryGetValue(key, out var e);

            yield return new HalfHourBucket
            {
                Timestamp = key,
                SolarKwh = s?.SolarKwh ?? 0,
                HomeConsumptionKwh = s?.HomeConsumptionKwh ?? 0,
                BatteryChargeKwh = s?.BatteryChargeKwh ?? 0,
                BatteryDischargeKwh = s?.BatteryDischargeKwh ?? 0,
                GridImportKwh = s?.GridImportKwh ?? 0,
                GridExportKwh = s?.GridExportKwh ?? 0,
                EvChargingKwh = e?.EvChargingKwh ?? 0
            };
        }
    }


    private SolarBucket AggregateSolar(IEnumerable<SolarVendorInterval> g)
    {
        return new SolarBucket
        {
            SolarKwh = g.Sum(x => x.EnergyProducedWh) / 1000m,
            HomeConsumptionKwh = g.Sum(x => x.EnergyConsumedWh) / 1000m,
            BatteryChargeKwh = g.Sum(x => x.StoredInBatteriesWh) / 1000m,
            BatteryDischargeKwh = g.Sum(x => x.DischargedFromBatteriesWh) / 1000m,
            GridImportKwh = g.Sum(x => x.ImportedFromGridWh) / 1000m,
            GridExportKwh = g.Sum(x => x.ExportedToGridWh) / 1000m
        };
    }

    private EvBucket AggregateEv(IEnumerable<EvCircuitReading> g)
    {
        return new EvBucket
        {
            EvChargingKwh = g.Sum(x => x.KiloWattHours)
        };
    }

    // Add these private container types so references in this file resolve

    private class EvBucket
    {
        public decimal EvChargingKwh { get; set; }
    }

    private class HalfHourBucket
    {
        public DateTime Timestamp { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal HomeConsumptionKwh { get; set; }
        public decimal BatteryChargeKwh { get; set; }
        public decimal BatteryDischargeKwh { get; set; }
        public decimal GridImportKwh { get; set; }
        public decimal GridExportKwh { get; set; }
        public decimal EvChargingKwh { get; set; }
    }
}
