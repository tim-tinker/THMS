using CsvHelper;
using System.Globalization;
using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Importers.Energy
{
    public class EvChargerImporter
    {
        private readonly EnergyDataStore _store;

        public EvChargerImporter(EnergyDataStore store)
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
                    Timestamp = csv.GetField<DateTime>("Start"),
                    EvChargingWh = csv.GetField<decimal>("Energy Used (Wh)"),
                    CommercialChargingCost = csv.GetField<decimal>("Cost"),
                };

                _store.AddEvChargingInterval(interval);
            }
        }
    }
}
