using System.Globalization;
using CsvHelper;
using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Ingestion.Importers.Energy
{
    public class HomeEvCircuitImporter
    {
        private readonly IEnergyDataStore _store;

        public HomeEvCircuitImporter(IEnergyDataStore store)
        {
            _store = store;
        }

        public IEnumerable<EvCircuitReading> Import(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var timeStamp = csv.GetField<DateTime>(0);

                var circuitEnergyKwh = csv.GetField<decimal>(1);

                var reading = new EvCircuitReading
                {
                    Timestamp = timeStamp,
                    KiloWattHours = circuitEnergyKwh,
                };
                yield return reading;
            }
        }
    }
}

