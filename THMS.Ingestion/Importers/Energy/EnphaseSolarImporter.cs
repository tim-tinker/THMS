using System.Globalization;
using CsvHelper;
using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Ingestion.Importers.Energy
{
    public class EnphaseSolarImporter
    {
        private readonly IEnergyDataStore _store;
        private readonly List<SolarVendorInterval> _intervals = [];

        public IEnumerable<SolarVendorInterval> Intervals => _intervals;
        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int IntervalCount => _intervals.Count;
        public string ErrorMessage { get; private set; }

        public EnphaseSolarImporter(IEnergyDataStore store)
        {
            _store = store;
        }

        public void Import(string csvPath)
        {
            _intervals.Clear();
            StartDate = DateTime.MinValue;
            EndDate = DateTime.MinValue;
            ErrorMessage = string.Empty;

            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            if (7 != csv.HeaderRecord.Length)
            {
                ErrorMessage = "Unexpected number of columns in CSV file.";
            }
            else if ("Date/Time" != csv.HeaderRecord[0]
                || "Energy Produced (Wh)" != csv.HeaderRecord[1]
                || "Energy Consumed (Wh)" != csv.HeaderRecord[2]
                || "Exported to Grid (Wh)" != csv.HeaderRecord[3]
                || "Imported from Grid (Wh)" != csv.HeaderRecord[4]
                || "Stored in batteries (Wh)" != csv.HeaderRecord[5]
                || "Discharged from batteries (Wh)" != csv.HeaderRecord[6])
            {
                ErrorMessage = "Unexpected column names in CSV file.";
            }
            else
            {
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

                    if (StartDate == DateTime.MinValue)
                    {
                        StartDate = interval.Timestamp;
                    }

                    EndDate = interval.Timestamp;

                    _intervals.Add(interval);
                }
            }
        }
    }
}
