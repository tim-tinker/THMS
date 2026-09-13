using System.Globalization;
using ExcelDataReader;

namespace THMS.Ingestion.Importers.Transportation
{
    public class SpreadsheetEvChargeSessionImporter
    {
        public SpreadsheetEvChargeSessionImporter()
        {
            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);
        }

        public List<ParsedSpreadsheetEvChargeSession> Parse(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = CreateReader(stream, filePath);
            SeekDataSheet(reader, filePath);

            var rows = new List<ParsedSpreadsheetEvChargeSession>();
            var headers = (IReadOnlyDictionary<string, int>?)null;
            var rowIndex = 0;

            while (reader.Read())
            {
                rowIndex++;
                if (headers is null)
                {
                    headers = ReadHeaders(reader);
                    continue;
                }

                var parsed = ReadRow(reader, headers);
                if (parsed is not null)
                    rows.Add(parsed);
            }

            return rows;
        }

        private static IExcelDataReader CreateReader(Stream stream, string filePath)
        {
            if (IsCsv(filePath))
                return ExcelReaderFactory.CreateCsvReader(stream);

            return ExcelReaderFactory.CreateReader(stream);
        }

        private static void SeekDataSheet(IExcelDataReader reader, string filePath)
        {
            if (IsCsv(filePath))
                return;

            do
            {
                if (string.Equals(reader.Name, "Data", StringComparison.OrdinalIgnoreCase))
                    return;
            } while (reader.NextResult());
        }

        private static bool IsCsv(string filePath) =>
            Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase);

        private static Dictionary<string, int> ReadHeaders(IExcelDataReader reader)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = NormalizeHeader(ReadString(reader, i));
                if (name.Length > 0 && !headers.ContainsKey(name))
                    headers[name] = i;
            }

            return headers;
        }

        private static string NormalizeHeader(string value) =>
            value.Replace("(kW)", "", StringComparison.OrdinalIgnoreCase)
                .Replace("(kwh)", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

        private static ParsedSpreadsheetEvChargeSession? ReadRow(
            IExcelDataReader reader,
            IReadOnlyDictionary<string, int> headers)
        {
            var date = ReadDate(reader, Column(headers, "Date"));
            var startClock = ReadTime(reader, Column(headers, "Start time", "StartTime"));
            var endClock = ReadTime(reader, Column(headers, "End time", "EndTime"));
            var odometer = ReadDecimal(reader, Column(headers, "Odometer"));
            var startSoc = ReadSocPercent(reader, Column(headers, "Start SOC", "StartSOC"));
            var endSoc = ReadSocPercent(reader, Column(headers, "End SOC", "EndSOC"));
            if (date is null || startClock is null || endClock is null
                || odometer is null || startSoc is null || endSoc is null)
            {
                return null;
            }

            var startTime = date.Value.Date + startClock.Value;
            var endTime = date.Value.Date + endClock.Value;
            if (endTime < startTime)
                endTime = endTime.AddDays(1);

            var charger = ReadString(reader, Column(headers, "Charger", "Charger (kW)"));
            if (string.IsNullOrWhiteSpace(charger))
                return null;

            return new ParsedSpreadsheetEvChargeSession
            {
                StartTime = startTime,
                EndTime = endTime,
                OdometerMiles = odometer.Value,
                StartSoc = startSoc.Value,
                EndSoc = endSoc.Value,
                KwhDrawn = ReadDecimal(reader, Column(headers, "kWh Added", "kWhAdded")) ?? 0,
                KwhAdded = ReadDecimal(reader, Column(headers, "battery kwh", "battery kWh", "BatteryKwh")) ?? 0,
                CostAdded = ReadDecimal(reader, Column(headers, "Cost Added", "CostAdded", "Cost")) ?? 0,
                IsHomeCharge = charger.Equals("Home", StringComparison.OrdinalIgnoreCase),
                Charger = charger
            };
        }

        private static int Column(IReadOnlyDictionary<string, int> headers, params string[] names)
        {
            foreach (var name in names)
            {
                if (headers.TryGetValue(name, out var index))
                    return index;
            }

            return -1;
        }

        private static string ReadString(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return "";
            return reader.GetValue(index)?.ToString()?.Trim() ?? "";
        }

        private static DateTime? ReadDate(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return null;

            return reader.GetValue(index) switch
            {
                DateTime date => date.Date,
                double oa when oa >= 1 => DateTime.FromOADate(oa).Date,
                int oa when oa >= 1 => DateTime.FromOADate(oa).Date,
                string text when TryParseDate(text, out var parsed) => parsed,
                _ => null
            };
        }

        private static TimeSpan? ReadTime(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return null;

            return reader.GetValue(index) switch
            {
                DateTime date => date.TimeOfDay,
                TimeSpan clock => clock,
                double fraction when fraction >= 0 && fraction < 1 => TimeSpan.FromDays(fraction),
                double oa when oa >= 1 => DateTime.FromOADate(oa).TimeOfDay,
                string text when TryParseTime(text, out var clock) => clock,
                _ => null
            };
        }

        private static decimal? ReadDecimal(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return null;

            var value = reader.GetValue(index);
            switch (value)
            {
                case null:
                case DBNull:
                    return null;
                case decimal number:
                    return number;
                case double number:
                    return (decimal)number;
                case int number:
                    return number;
                case string text when string.IsNullOrWhiteSpace(text):
                    return null;
                case string text when decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                    return parsed;
                case string text when decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var parsed):
                    return parsed;
                default:
                    return null;
            }
        }

        private static bool TryParseDate(string text, out DateTime date)
        {
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                || DateTime.TryParse(text, out parsed))
            {
                date = parsed.Date;
                return true;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var oa) && oa >= 1)
            {
                date = DateTime.FromOADate(oa).Date;
                return true;
            }

            date = default;
            return false;
        }

        private static bool TryParseTime(string text, out TimeSpan clock)
        {
            if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out clock)
                || TimeSpan.TryParse(text, out clock))
            {
                return true;
            }

            if (DateTime.TryParse(text, out var parsed))
            {
                clock = parsed.TimeOfDay;
                return true;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var fraction)
                && fraction >= 0
                && fraction < 1)
            {
                clock = TimeSpan.FromDays(fraction);
                return true;
            }

            clock = default;
            return false;
        }

        private static decimal? ReadSocPercent(IExcelDataReader reader, int index)
        {
            var value = ReadDecimal(reader, index);
            if (value is null)
                return null;
            return value.Value <= 1 ? value.Value * 100m : value.Value;
        }
    }
}
