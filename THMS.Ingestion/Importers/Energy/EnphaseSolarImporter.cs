using System.Globalization;
using CsvHelper;
using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Ingestion.Importers.Energy
{
    public class EnphaseSolarImporter
    {
        private readonly List<SolarProductionInterval> _intervals = [];

        public IEnumerable<SolarProductionInterval> Intervals => _intervals;
        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int IntervalCount => _intervals.Count;
        public string ErrorMessage { get; private set; } = "";

        public EnphaseSolarImporter()
        {
        }

        public EnphaseSolarImporter(IEnergyDataStore _)
        {
        }

        public List<SolarProductionInterval> Parse(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            if (csv.HeaderRecord is null || csv.HeaderRecord.Length != 7)
                throw new InvalidDataException("Unexpected number of columns in CSV file.");
            if (csv.HeaderRecord[0] != "Date/Time"
                || csv.HeaderRecord[1] != "Energy Produced (Wh)"
                || csv.HeaderRecord[2] != "Energy Consumed (Wh)"
                || csv.HeaderRecord[3] != "Exported to Grid (Wh)"
                || csv.HeaderRecord[4] != "Imported from Grid (Wh)"
                || csv.HeaderRecord[5] != "Stored in batteries (Wh)"
                || csv.HeaderRecord[6] != "Discharged from batteries (Wh)")
                throw new InvalidDataException("Unexpected column names in CSV file.");

            var intervals = new List<SolarProductionInterval>();
            while (csv.Read())
            {
                intervals.Add(new SolarProductionInterval
                {
                    Timestamp = csv.GetField<DateTime>("Date/Time"),
                    EnergyProducedWh = csv.GetField<decimal>("Energy Produced (Wh)"),
                    EnergyConsumedWh = csv.GetField<decimal>("Energy Consumed (Wh)"),
                    ExportedToGridWh = csv.GetField<decimal>("Exported to Grid (Wh)"),
                    ImportedFromGridWh = csv.GetField<decimal>("Imported from Grid (Wh)"),
                    StoredInBatteriesWh = csv.GetField<decimal>("Stored in batteries (Wh)"),
                    DischargedFromBatteriesWh = csv.GetField<decimal>("Discharged from batteries (Wh)")
                });
            }

            return intervals;
        }

        public void Import(string csvPath)
        {
            try
            {
                var parsed = Parse(csvPath);
                _intervals.AddRange(parsed);
                if (parsed.Count > 0)
                {
                    var start = parsed.Min(i => i.Timestamp);
                    var end = parsed.Max(i => i.Timestamp);
                    StartDate = StartDate == DateTime.MinValue ? start : Min(StartDate, start);
                    EndDate = EndDate == DateTime.MinValue ? end : Max(EndDate, end);
                }

                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private static DateTime Min(DateTime left, DateTime right) => left <= right ? left : right;
        private static DateTime Max(DateTime left, DateTime right) => left >= right ? right : left;
    }
}
