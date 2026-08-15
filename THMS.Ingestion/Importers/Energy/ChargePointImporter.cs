using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Domain.Finance;

namespace THMS.Ingestion.Importers.Energy
{
    public class ChargePointImporter : BaseCommercialChargerImporter
    {
        public ChargePointImporter(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore)
        {
            VehicleStore = vehicleStore;
            FinanceStore = financeStore;
        }

        protected override IEnumerable<CommercialEvChargeSession> ReadChargeSessions(string filePath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            while (csv.Read())
            {
                var startString = csv.GetField("Start");
                var endString = csv.GetField("End");
                var durationString = csv.GetField("Duration");
                var kwhString = csv.GetField("Energy (kWh)");
                var costString = csv.GetField("Cost");
                var stationString = csv.GetField("Station");

                var startTime = ParseChargePointDateTime(startString);
                var endTime = ParseChargePointDateTime(endString);
                var duration = ParseDuration(durationString);

                var totalKwh = decimal.Parse(kwhString, CultureInfo.InvariantCulture);
                var totalCost = decimal.Parse(costString, CultureInfo.InvariantCulture);

                var evChargeSession = new CommercialEvChargeSession
                {
                    Id = Guid.NewGuid(),
                    StartTime = startTime,
                    EndTime = endTime,
                    KwhDrawn = totalKwh,          // kWh, not Wh
                    SessionCost = totalCost,     // ChargePoint always provides cost
                };

                yield return evChargeSession;
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
