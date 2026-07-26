using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Importers
{
    public class ChargePointImporter
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly IFinanceDataStore _financeStore;

        public ChargePointImporter(
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore)
        {
            _energyStore = energyStore;
            _financeStore = financeStore;
        }

        public void ImportFromFile(string csvFilePath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
            };

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, config);

            while (csv.Read())
            {
                var startString = csv.GetField("Start");
                var endString = csv.GetField("End");
                var durationString = csv.GetField("Duration");
                var kwhString = csv.GetField("Energy (kWh)");
                var costString = csv.GetField("Cost");

                var startTime = ParseChargePointDateTime(startString);
                var endTime = ParseChargePointDateTime(endString);
                var duration = ParseDuration(durationString);

                var totalKwh = decimal.Parse(kwhString, CultureInfo.InvariantCulture);
                var totalCost = decimal.Parse(costString, CultureInfo.InvariantCulture);

                // -----------------------------------------
                // ENERGY PORTION (Wh)
                // -----------------------------------------
                var energyRecord = new EvCommercialChargingSession
                {
                    Timestamp = startTime,
                    Duration = duration,
                    EvChargingWh = (int)(totalKwh * 1000m),
                    Source = "ChargePoint"
                };

                _energyStore.AddEvChargingSession(energyRecord);

                // -----------------------------------------
                // FINANCIAL PORTION ($)
                // -----------------------------------------
                var costRecord = new CommercialChargingCostRecord
                {
                    Timestamp = startTime,
                    Cost = totalCost,
                    Vendor = "ChargePoint",
                    SessionId = null
                };

                _financeStore.AddCommercialChargingCostRecord(costRecord);
            }
        }

        private static DateTime ParseChargePointDateTime(string value)
        {
            // Example: "7/8/2026, 5:15 PM CDT"
            // Remove timezone abbreviation and parse as local time.
            var cleaned = value
                .Replace(" CDT", "")
                .Replace(" CST", "")
                .Replace(" PST", "")
                .Replace(" EST", "")
                .Trim();

            return DateTime.Parse(cleaned, CultureInfo.InvariantCulture);
        }

        private static TimeSpan ParseDuration(string value)
        {
            // Example: "2h 42m 25s"
            int hours = 0, minutes = 0, seconds = 0;

            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.EndsWith("h", StringComparison.OrdinalIgnoreCase))
                    hours = int.Parse(part[..^1], CultureInfo.InvariantCulture);
                else if (part.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                    minutes = int.Parse(part[..^1], CultureInfo.InvariantCulture);
                else if (part.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                    seconds = int.Parse(part[..^1], CultureInfo.InvariantCulture);
            }

            return new TimeSpan(hours, minutes, seconds);
        }
    }
}
