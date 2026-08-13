using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Energy;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteEnergyDataStore : IEnergyDataStore
    {
        private readonly string _connectionString;

        private readonly HomeCircuitReadingsTable _homeCircuitReadingsTable = new();
        private readonly HomeCircuitAttributionTable _homeCircuitAttributionTable = new();
        private readonly SolarVendorIntervalsTable _solarTable = new();

        public SQLiteEnergyDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = OpenConnection();
            InitializeSchema(conn);
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void InitializeSchema(SqliteConnection conn)
        {
            _homeCircuitReadingsTable.InitializeSchema(conn);
            _homeCircuitAttributionTable.InitializeSchema(conn);
            _solarTable.InitializeSchema(conn);
        }

        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        public void UpsertHomeCircuitReading(HomeCircuitReading reading)
        {
            using var conn = OpenConnection();
            _homeCircuitReadingsTable.Upsert(conn, reading);
        }

        public IEnumerable<HomeCircuitReading> GetHomeCircuitReadings(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _homeCircuitReadingsTable.GetRange(conn, start, end);
        }

        public HomeCircuitReading? GetLatestHomeCircuitReading()
        {
            using var conn = OpenConnection();
            return _homeCircuitReadingsTable.GetLatest(conn);
        }

        // ---------------------------------------------------------
        // HOME SOLAR VENDOR INTERVALS
        // ---------------------------------------------------------

        public void UpsertSolarVendorInterval(SolarVendorInterval interval)
        {
            using var conn = OpenConnection();
            _solarTable.Upsert(conn, interval);
        }

        public IEnumerable<SolarVendorInterval> GetSolarVendorIntervals(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _solarTable.GetRange(conn, start, end);
        }

        public SolarVendorInterval? GetLatestSolarVendorInterval()
        {
            using var conn = OpenConnection();
            return _solarTable.GetLatest(conn);
        }

        // ---------------------------------------------------------
        // EV ATTRIBUTION
        // ---------------------------------------------------------

        public void UpsertHomeCircuitAttribution(HomeCircuitAttribution result)
        {
            using var conn = OpenConnection();
            _homeCircuitAttributionTable.Upsert(conn, result);
        }

        public IReadOnlyCollection<HomeCircuitAttribution> GetHomeCircuitAttribution(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _homeCircuitAttributionTable.GetRange(conn, start, end);
        }

        public HomeCircuitAttribution? GetLatestHomeCircuitAttribution()
        {
            using var conn = OpenConnection();
            return _homeCircuitAttributionTable.GetLatest(conn);
        }
    }
}
