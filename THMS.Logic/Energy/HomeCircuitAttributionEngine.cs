using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Logic.Energy;

public class HomeCircuitAttributionEngine
{
    private readonly IEnergyDataStore _store;
    private readonly List<HomeCircuitAttribution> _results = [];

    public IEnumerable<HomeCircuitAttribution> Results => _results;
    public int ResultCount => _results.Count;

    public HomeCircuitAttributionEngine(IEnergyDataStore store)
    {
        _store = store;
    }

    public void Compute(DateTime start, DateTime end)
    {
        _results.Clear();

        // Raw data
        var intervals = _store.GetSolarProductionIntervals(start, end).ToList();
        var readings = _store.GetHomeCircuitReadings(start, end).ToList();

        // Half-hour buckets
        var buckets = HalfHourBucketJoin(intervals, readings).ToList();

        foreach (var b in buckets)
        {
            decimal circuitKwh = b.CircuitDrawKwh;

            if (circuitKwh <= 0)
                continue;

            var otherHomeConsumptionKwh = b.HomeConsumptionKwh - circuitKwh;
            // Available sources
            decimal solarAvailable =
                b.SolarKwh
                - otherHomeConsumptionKwh
                - b.BatteryChargeKwh;

            if (solarAvailable < 0)
                solarAvailable = 0;

            decimal gridAvailable = b.GridImportKwh;

            // Attribution
            decimal gridToCircuit = Math.Min(circuitKwh, gridAvailable);
            decimal remaining = circuitKwh - gridToCircuit;

            decimal solarToCircuit = Math.Min(remaining, solarAvailable);
            remaining -= solarToCircuit;
            decimal batteryToCircuit = remaining;

            // Store result
            var result = new HomeCircuitAttribution
            {
                Timestamp = b.Timestamp,
                TotalWh = circuitKwh * 1000m,
                SolarWh = solarToCircuit * 1000m,
                BatteryWh = batteryToCircuit * 1000m,
                GridWh = gridToCircuit * 1000m
            };

            _results.Add(result);
        }
    }

    private IEnumerable<HalfHourBucket> HalfHourBucketJoin(
        IEnumerable<SolarProductionInterval> solar,
        IEnumerable<HomeCircuitReading> readings)
    {
        // Step 1: bucket solar intervals
        var solarBuckets = solar
            .GroupBy(i => TimeBucket.GetHalfHour(i.Timestamp))
            .ToDictionary(g => g.Key, g => AggregateSolar(g));

        // Step 2: bucket EV intervals
        var circuitBuckets = readings
            .GroupBy(i => TimeBucket.GetHalfHour(i.Timestamp))
            .ToDictionary(g => g.Key, g => AggregateEv(g));

        // Step 3: join buckets
        var keys = solarBuckets.Keys.Union(circuitBuckets.Keys).OrderBy(k => k);

        foreach (var key in keys)
        {
            solarBuckets.TryGetValue(key, out var interval);
            circuitBuckets.TryGetValue(key, out var reading);

            yield return new HalfHourBucket
            {
                Timestamp = key,
                SolarKwh = interval?.SolarKwh ?? 0,
                HomeConsumptionKwh = interval?.HomeConsumptionKwh ?? 0,
                BatteryChargeKwh = interval?.BatteryChargeKwh ?? 0,
                BatteryDischargeKwh = interval?.BatteryDischargeKwh ?? 0,
                GridImportKwh = interval?.GridImportKwh ?? 0,
                GridExportKwh = interval?.GridExportKwh ?? 0,
                CircuitDrawKwh = reading?.CircuitDrawKwh ?? 0
            };
        }
    }


    private SolarBucket AggregateSolar(IEnumerable<SolarProductionInterval> g)
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

    private EvBucket AggregateEv(IEnumerable<HomeCircuitReading> g)
    {
        return new EvBucket
        {
            CircuitDrawKwh = g.Sum(x => x.KiloWattHours)
        };
    }

    // Add these private container types so references in this file resolve

    private class EvBucket
    {
        public decimal CircuitDrawKwh { get; set; }
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
        public decimal CircuitDrawKwh { get; set; }
    }
}
