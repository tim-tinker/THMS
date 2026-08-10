using System.Globalization;
using CsvHelper;
using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Ingestion.Importers.Energy
{
    public class HomeEvCircuitImporter
    {
        private readonly IEnergyDataStore _store;
        private readonly List<EvCircuitReading> _readings = [];

        public IEnumerable<EvCircuitReading> Readings => _readings;
        public DateTime StartDate { get; private set; } = DateTime.MinValue;
        public DateTime EndDate { get; private set; } = DateTime.MinValue;
        public int ReadingCount => _readings.Count;
        public string ErrorMessage { get; private set; }

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
            if (2 != csv.HeaderRecord.Length)
            {
                ErrorMessage = "Unexpected number of columns in CSV file.";
            }
            else if ("Local SPAN Panel time (America/Chicago)" != csv.HeaderRecord[0]
                || "Energy Data (Wh)" != csv.HeaderRecord[1])
            {
                ErrorMessage = "Unexpected column names in CSV file.";
            }
            else
            {
                while (csv.Read())
                {
                    var timeStamp = csv.GetField<DateTime>(0);

                    var circuitEnergyKwh = csv.GetField<decimal>(1);

                    var reading = new EvCircuitReading
                    {
                        Timestamp = timeStamp,
                        KiloWattHours = circuitEnergyKwh,
                    };

                    if (StartDate == DateTime.MinValue)
                    {
                        StartDate = timeStamp;
                    }

                    EndDate = timeStamp;

                    _readings.Add(reading);
                }
            }
        }
    }
}

