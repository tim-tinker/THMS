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

        public void Import(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var firstTimeStamp = csv.GetField<DateTime>("Local SPAN Panel time (America/Chicago)");

                var circuitEnergyWh = csv.GetField<decimal>("Energy Data (Wh)");

                var reading = new EvCircuitReading
                {
                    Timestamp = firstTimeStamp,
                    CircuitUseWh = circuitEnergyWh,
                };
                _store.AddEvCircuitReading(reading);
            }
        }
    }
}

