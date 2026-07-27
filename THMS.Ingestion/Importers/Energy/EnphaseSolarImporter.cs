using System.Globalization;
using CsvHelper;
using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Ingestion.Importers.Energy
{
    public class EnphaseSolarImporter
    {
        private readonly IEnergyDataStore _store;

        public EnphaseSolarImporter(IEnergyDataStore store)
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
                var interval = new SolarVendorInterval
                {
                    Timestamp = csv.GetField<DateTime>("Date/Time"),
                    EnergyProducedWh = csv.GetField<decimal>("Energy Produced (Wh)"),
                    EnergyConsumedWh = csv.GetField<decimal>("Energy Consumed (Wh)"),
                    ExportedToGridWh = csv.GetField<decimal>("Exported to Grid (Wh)"),
                    ImportedFromGridWh = csv.GetField<decimal>("Imported from Grid (Wh)"),
                    StoredInBatteriesWh = csv.GetField<decimal>("Stored in batteries (Wh)"),
                    DischargedFromBatteriesWh = csv.GetField<decimal>("Discharged from batteries (Wh)")
                };

                _store.AddSolarVendorInterval(interval);
            }
        }
    }
}
