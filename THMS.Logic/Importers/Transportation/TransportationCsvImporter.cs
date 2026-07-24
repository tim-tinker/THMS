using System.Globalization;
using THMS.Domain;
using THMS.Domain.Transportation;

namespace THMS.Importers.Transportation
{
    public class TransportationCsvImporter : ITransportationImporter
    {
        public IEnumerable<Vehicle> ImportVehicles(string filePath)
        {
            var vehicles = new List<Vehicle>();

            foreach (var line in File.ReadLines(filePath).Skip(1)) // skip header
            {
                var parts = line.Split(',');

                var name = parts[0];

                var homePct = decimal.Parse(parts[1], CultureInfo.InvariantCulture);
                var publicPct = decimal.Parse(parts[2], CultureInfo.InvariantCulture);
                var regenPct = decimal.Parse(parts[3], CultureInfo.InvariantCulture);

                var monthlyCosts = new List<MonthlyValue>();
                for (int i = 4; i < parts.Length; i++)
                {
                    monthlyCosts.Add(new MonthlyValue
                    {
                        Month = MonthlyValue.MonthNames[i - 4],
                        Amount = decimal.Parse(parts[i], CultureInfo.InvariantCulture)
                    });
                }

                var vehicle = DomainFactory.CreateVehicle(
                    name,
                    new EnergyBreakdown(homePct, publicPct, regenPct),
                    monthlyCosts
                );

                vehicles.Add(vehicle);
            }

            return vehicles;
        }
    }
}
