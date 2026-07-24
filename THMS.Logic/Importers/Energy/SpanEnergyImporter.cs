using System.Globalization;
using CsvHelper;
using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Logic.Importers.Energy
{
    public class SpanEnergyImporter
    {
        private readonly EnergyDataStore _store;

        public SpanEnergyImporter(EnergyDataStore store)
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
                var interval = new EvChargingInterval
                {
                    Timestamp = csv.GetField<DateTime>("Local SPAN Panel time (America/Chicago)"),
                    EvChargingWh = csv.GetField<decimal>("Energy Data (Wh)"),
                };
                _store.AddEvChargingInterval(interval);
            }
        }
    }
}

