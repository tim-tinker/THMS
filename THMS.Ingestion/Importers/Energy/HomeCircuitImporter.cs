using System.Globalization;
using CsvHelper;
using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Ingestion.Importers.Energy
{
    public class HomeCircuitImporter
    {
        private readonly List<HomeCircuitReading> _readings = [];

        public IEnumerable<HomeCircuitReading> Readings => _readings;
        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int ReadingCount => _readings.Count;
        public string ErrorMessage { get; private set; } = "";

        public HomeCircuitImporter()
        {
        }

        public HomeCircuitImporter(IEnergyDataStore _)
        {
        }

        public List<HomeCircuitReading> Parse(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            if (csv.HeaderRecord is null || csv.HeaderRecord.Length != 2)
                throw new InvalidDataException("Unexpected number of columns in CSV file.");
            if (csv.HeaderRecord[0] != "Local SPAN Panel time (America/Chicago)"
                || csv.HeaderRecord[1] != "Energy Data (Wh)")
                throw new InvalidDataException("Unexpected column names in CSV file.");

            var readings = new List<HomeCircuitReading>();
            while (csv.Read())
            {
                var kiloWattHours = csv.GetField<decimal>(1);
                if (kiloWattHours == 0)
                    continue;

                readings.Add(new HomeCircuitReading
                {
                    Timestamp = csv.GetField<DateTime>(0),
                    KiloWattHours = kiloWattHours
                });
            }

            return readings;
        }

        public void Import(string csvPath)
        {
            try
            {
                var parsed = Parse(csvPath);
                _readings.AddRange(parsed);
                if (parsed.Count > 0)
                {
                    var start = parsed.Min(r => r.Timestamp);
                    var end = parsed.Max(r => r.Timestamp);
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
