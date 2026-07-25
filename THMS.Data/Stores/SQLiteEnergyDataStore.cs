using Microsoft.Data.Sqlite;
using THMS.Domain.Energy;

namespace THMS.Data.Stores
{
    public class SQLiteEnergyDataStore : IEnergyDataStore
    {
        private readonly string _connectionString;

        public SQLiteEnergyDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            InitializeSchema();
        }

        private void InitializeSchema()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EvChargingSessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                EvChargingWh REAL NOT NULL,
                CommercialChargingCost REAL
            );

            CREATE TABLE IF NOT EXISTS EvCircuitReadings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                CircuitUseWh REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS SolarVendorIntervals (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                EnergyProducedWh REAL NOT NULL,
                EnergyConsumedWh REAL NOT NULL,
                ExportedToGridWh REAL NOT NULL,
                ImportedFromGridWh REAL NOT NULL,
                StoredInBatteriesWh REAL NOT NULL,
                DischargedFromBatteriesWh REAL NOT NULL
            );
            ";

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // EV Circuit Readings (SPAN)
        // ---------------------------------------------------------

        public void AddEvCircuitReading(EvCircuitReading reading)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO EvCircuitReadings (Timestamp, CircuitUseWh)
            VALUES ($ts, $wh);
            ";

            cmd.Parameters.AddWithValue("$ts", reading.Timestamp);
            cmd.Parameters.AddWithValue("$wh", reading.CircuitUseWh);

            cmd.ExecuteNonQuery();
        }

        public IReadOnlyCollection<EvCircuitReading> GetEvCircuitReadings()
        {
            var list = new List<EvCircuitReading>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT Timestamp, CircuitUseWh FROM EvCircuitReadings";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new EvCircuitReading
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    CircuitUseWh = reader.GetDecimal(1)
                });
            }

            return list;
        }

        public IReadOnlyCollection<EvCircuitReading> GetAllEvCircuitReadingsRaw()
            => GetEvCircuitReadings();

        // ---------------------------------------------------------
        // EV Charging Sessions (ChargePoint)
        // ---------------------------------------------------------

        public void AddEvChargingSession(EvChargingSession session)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO EvChargingSessions
            (Timestamp, EvChargingWh, CommercialChargingCost)
            VALUES ($ts, $wh, $cost);
            ";

            cmd.Parameters.AddWithValue("$ts", session.Timestamp);
            cmd.Parameters.AddWithValue("$wh", session.EvChargingWh);
            cmd.Parameters.AddWithValue("$cost", session.CommercialChargingCost ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IReadOnlyCollection<EvChargingSession> GetEvChargingSessions()
        {
            var list = new List<EvChargingSession>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT Timestamp, EvChargingWh, CommercialChargingCost FROM EvChargingSessions";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new EvChargingSession
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    EvChargingWh = reader.GetDecimal(1),
                    CommercialChargingCost = reader.IsDBNull(2)
                        ? null
                        : reader.GetDecimal(2)
                });
            }

            return list;
        }

        public IReadOnlyCollection<EvChargingSession> GetAllEvChargingSessionsRaw()
            => GetEvChargingSessions();

        // ---------------------------------------------------------
        // Solar Vendor Intervals
        // ---------------------------------------------------------

        public void AddSolarVendorInterval(SolarVendorInterval interval)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO SolarVendorIntervals
            (Timestamp, EnergyProducedWh, EnergyConsumedWh,
             ExportedToGridWh, ImportedFromGridWh,
             StoredInBatteriesWh, DischargedFromBatteriesWh)
            VALUES ($ts, $produced, $consumed,
                    $exported, $imported,
                    $stored, $discharged);
            ";

            cmd.Parameters.AddWithValue("$ts", interval.Timestamp);
            cmd.Parameters.AddWithValue("$produced", interval.EnergyProducedWh);
            cmd.Parameters.AddWithValue("$consumed", interval.EnergyConsumedWh);
            cmd.Parameters.AddWithValue("$exported", interval.ExportedToGridWh);
            cmd.Parameters.AddWithValue("$imported", interval.ImportedFromGridWh);
            cmd.Parameters.AddWithValue("$stored", interval.StoredInBatteriesWh);
            cmd.Parameters.AddWithValue("$discharged", interval.DischargedFromBatteriesWh);

            cmd.ExecuteNonQuery();
        }

        public IReadOnlyCollection<SolarVendorInterval> GetSolarVendorIntervals()
        {
            var list = new List<SolarVendorInterval>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                     @"SELECT Timestamp, EnergyProducedWh, EnergyConsumedWh,
                              ExportedToGridWh, ImportedFromGridWh,
                              StoredInBatteriesWh, DischargedFromBatteriesWh
                      FROM SolarVendorIntervals";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SolarVendorInterval
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    EnergyProducedWh = reader.GetDecimal(1),
                    EnergyConsumedWh = reader.GetDecimal(2),
                    ExportedToGridWh = reader.GetDecimal(3),
                    ImportedFromGridWh = reader.GetDecimal(4),
                    StoredInBatteriesWh = reader.GetDecimal(5),
                    DischargedFromBatteriesWh = reader.GetDecimal(6)
                });
            }

            return list;
        }

        public IReadOnlyCollection<SolarVendorInterval> GetAllSolarVendorIntervalsRaw()
            => GetSolarVendorIntervals();
    }
}
