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

        private readonly EvCircuitReadingsTable _evCircuitReadingsTable = new();
        private readonly EvCommercialChargeSessionsTable _evCommercialSessionsTable = new();
        private readonly EvCircuitSegmentsTable _evCircuitSegmentsTable = new();
        private readonly EvAttributionTable _evAttrTable = new();
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
            _evCircuitReadingsTable.InitializeSchema(conn);
            _evCommercialSessionsTable.InitializeSchema(conn);
            _evCircuitSegmentsTable.InitializeSchema(conn);
            _evAttrTable.InitializeSchema(conn);
            _solarTable.InitializeSchema(conn);
        }

        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        public void UpsertEvCircuitReading(EvCircuitReading reading)
        {
            using var conn = OpenConnection();
            _evCircuitReadingsTable.Upsert(conn, reading);
        }

        public IEnumerable<EvCircuitReading> GetEvCircuitReadings(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _evCircuitReadingsTable.GetRange(conn, start, end);
        }

        public EvCircuitReading? GetLatestEvCircuitReading()
        {
            using var conn = OpenConnection();
            return _evCircuitReadingsTable.GetLatest(conn);
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
        // COMMERCIAL CHARGING SESSIONS
        // ---------------------------------------------------------

        public void UpsertEvCommercialChargeSession(EvCommercialChargeSession session)
        {
            using var conn = OpenConnection();
            _evCommercialSessionsTable.Upsert(conn, session);
        }

        public IEnumerable<EvCommercialChargeSession> GetEvCommercialChargeSessions(
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            return _evCommercialSessionsTable.GetRange(conn, start, end);
        }

        // ----------------------------------------------------------
        // HOME EV CIRCUIT SEGMENTS
        // ----------------------------------------------------------

        public void SaveEvCircuitSegments(Guid sessionId, IEnumerable<EvCircuitSegment> segments)
        {
            using var conn = OpenConnection();
            _evCircuitSegmentsTable.SaveEvCircuitSegments(conn, sessionId, segments);
        }

        public IEnumerable<EvCircuitSegment> GetEvCircuitSegments(Guid sessionId)
        {
            using var conn = OpenConnection();
            return _evCircuitSegmentsTable.GetEvCircuitSegments(conn, sessionId);
        }

        public void DeleteEvCircuitSegments(Guid sessionId)
        {
            using var conn = OpenConnection();
            _evCircuitSegmentsTable.DeleteEvCircuitSegments(conn, sessionId);
        }

        public EvCircuitSegmentSummary GetEvCircuitSummary(Guid sessionId)
        {
            using var conn = OpenConnection();
            return _evCircuitSegmentsTable.GetEvCircuitSummary(conn, sessionId);
        }

        // ---------------------------------------------------------
        // EV ATTRIBUTION
        // ---------------------------------------------------------

        public void UpsertEvAttribution(EnergyAttributionResult result)
        {
            using var conn = OpenConnection();
            _evAttrTable.Upsert(conn, result);
        }

        public IReadOnlyCollection<EnergyAttributionResult> GetEvAttribution(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _evAttrTable.GetRange(conn, start, end);
        }

        public EnergyAttributionResult? GetLatestEvAttribution()
        {
            using var conn = OpenConnection();
            return _evAttrTable.GetLatest(conn);
        }
    }
}
