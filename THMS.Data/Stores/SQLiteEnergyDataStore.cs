using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Data.Stores.SqqlTables;
using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteEnergyDataStore : IEnergyDataStore
    {
        private readonly string _connectionString;

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
            _evCircuitSegmentsTable.InitializeSchema(conn);
            _evAttrTable.InitializeSchema(conn);
            _solarTable.InitializeSchema(conn);
        }

        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        public void AddEvCircuitReading(EvCircuitReading reading)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO EvCircuitReadings
                (Id, Timestamp, WattHours, CircuitId)
                VALUES
                (@Id, @Timestamp, @WattHours, @CircuitId);", conn);

            cmd.Parameters.AddWithValue("@Id", reading.Id.ToString());
            cmd.Parameters.AddWithValue("@Timestamp", reading.Timestamp);
            cmd.Parameters.AddWithValue("@WattHours", reading.KiloWattHours);
            cmd.Parameters.AddWithValue("@CircuitId", reading.CircuitId ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<EvCircuitReading> GetEvCircuitReadings(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, Timestamp, WattHours, CircuitId
                FROM EvCircuitReadings
                WHERE Timestamp >= @Start AND Timestamp <= @End
                ORDER BY Timestamp;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<EvCircuitReading>();

            while (reader.Read())
            {
                list.Add(new EvCircuitReading
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Timestamp = reader.GetDateTime(1),
                    KiloWattHours = reader.GetDecimal(2),
                    CircuitId = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return list;
        }

        // ---------------------------------------------------------
        // HOME SOLAR VENDOR INTERVALS
        // ---------------------------------------------------------

        public void AddSolarVendorInterval(SolarVendorInterval interval)
        {
            using var conn = OpenConnection();
            _solarTable.Insert(conn, interval);
        }

        public IEnumerable<SolarVendorInterval> GetSolarVendorIntervals(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _solarTable.GetRange(conn, start, end);
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING SESSIONS
        // ---------------------------------------------------------

        public void AddEvCommercialChargeSession(EvCommercialChargeSession session)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO EvCommercialChargeSessions
                (Id, StartTime, EndTime, KwhAdded, ChargeCost, VendorSessionId, Location)
                VALUES
                (@Id, @StartTime, @EndTime, @KwhAdded, @ChargeCost, @VendorSessionId, @Location);", conn);

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@EndTime", session.EndTime);
            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@ChargeCost", session.ChargeCost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorSessionId", session.VendorSessionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Location", session.Location ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<EvCommercialChargeSession> GetEvCommercialChargeSessions(
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, StartTime, EndTime, KwhAdded, ChargeCost, VendorSessionId, Location
                FROM EvCommercialChargeSessions
                WHERE StartTime >= @Start AND EndTime <= @End
                ORDER BY StartTime;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<EvCommercialChargeSession>();

            while (reader.Read())
            {
                list.Add(new EvCommercialChargeSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartTime = reader.GetDateTime(1),
                    EndTime = reader.GetDateTime(2),
                    KwhAdded = reader.GetDecimal(3),
                    ChargeCost = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    VendorSessionId = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Location = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return list;
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS
        // ---------------------------------------------------------

        public void AddCommercialChargeCostRecord(CommercialChargeCostRecord record)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO CommercialChargeCostRecords
                (Id, SessionId, Date, Cost, Vendor)
                VALUES
                (@Id, @SessionId, @Date, @Cost, @Vendor);", conn);

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@SessionId", record.SessionId);
            cmd.Parameters.AddWithValue("@Date", record.Date);
            cmd.Parameters.AddWithValue("@Cost", record.Cost);
            cmd.Parameters.AddWithValue("@Vendor", record.Vendor);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecords(
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, SessionId, Date, Cost, Vendor
                FROM CommercialChargeCostRecords
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<CommercialChargeCostRecord>();

            while (reader.Read())
            {
                list.Add(new CommercialChargeCostRecord
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    SessionId = reader.GetString(1),
                    Date = reader.GetDateTime(2),
                    Cost = reader.GetDecimal(3),
                    Vendor = reader.GetString(4)
                });
            }

            return list;
        }

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecordsByVendor(
            string vendor,
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, SessionId, Date, Cost, Vendor
                FROM CommercialChargeCostRecords
                WHERE Vendor = @Vendor
                AND Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@Vendor", vendor);
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<CommercialChargeCostRecord>();

            while (reader.Read())
            {
                list.Add(new CommercialChargeCostRecord
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    SessionId = reader.GetString(1),
                    Date = reader.GetDateTime(2),
                    Cost = reader.GetDecimal(3),
                    Vendor = reader.GetString(4)
                });
            }

            return list;
        }

        // ----------------------------------------------------------
        // HOME EV CIRCUIT SEGMENTS
        // ----------------------------------------------------------

        // Store segments for a session (overwrite existing)
        public void SaveEvCircuitSegments(Guid sessionId, IEnumerable<EvCircuitSegment> segments)
        {
            using var conn = OpenConnection();
            _evCircuitSegmentsTable.SaveEvCircuitSegments(conn, sessionId, segments);
        }

        // Retrieve segments for a session
        public IEnumerable<EvCircuitSegment> GetEvCircuitSegments(Guid sessionId)
        {
            using var conn = OpenConnection();
            return _evCircuitSegmentsTable.GetEvCircuitSegments(conn, sessionId);
        }

        // Delete all segments for a session
        public void DeleteEvCircuitSegments(Guid sessionId)
        {
            using var conn = OpenConnection();
            _evCircuitSegmentsTable.DeleteEvCircuitSegments(conn, sessionId);
        }

        // Optional convenience: roll-up summary
        public EvCircuitSegmentSummary GetEvCircuitSummary(Guid sessionId)
        {
            using var conn = OpenConnection();
            return _evCircuitSegmentsTable.GetEvCircuitSummary(conn, sessionId);
        }

        // ---------------------------------------------------------
        // EV ATTRIBUTION
        // ---------------------------------------------------------

        public void AddEvAttribution(EnergyAttributionResult result)
        {
            using var conn = OpenConnection();
            _evAttrTable.Insert(conn, result);
        }

        public IReadOnlyCollection<EnergyAttributionResult> GetEvAttribution(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _evAttrTable.GetRange(conn, start, end);
        }
    }
}
