using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteEnergyDataStore : IEnergyDataStore
    {
        private readonly string _connectionString;

        public SQLiteEnergyDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
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
            cmd.Parameters.AddWithValue("@WattHours", reading.WattHours);
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
                    WattHours = reader.GetDecimal(2),
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
            using var cmd = new SqliteCommand(@"
                INSERT INTO SolarVendorIntervals
                (Id, Timestamp, EnergyProducedWh, EnergyConsumedWh,
                 ExportedToGridWh, ImportedFromGridWh,
                 StoredInBatteriesWh, DischargedFromBatteriesWh)
                VALUES
                (@Id, @Timestamp, @EnergyProducedWh, @EnergyConsumedWh,
                 @ExportedToGridWh, @ImportedFromGridWh,
                 @StoredInBatteriesWh, @DischargedFromBatteriesWh);", conn);

            cmd.Parameters.AddWithValue("@Id", interval.Id.ToString());
            cmd.Parameters.AddWithValue("@Timestamp", interval.Timestamp);
            cmd.Parameters.AddWithValue("@EnergyProducedWh", interval.EnergyProducedWh);
            cmd.Parameters.AddWithValue("@EnergyConsumedWh", interval.EnergyConsumedWh);
            cmd.Parameters.AddWithValue("@ExportedToGridWh", interval.ExportedToGridWh);
            cmd.Parameters.AddWithValue("@ImportedFromGridWh", interval.ImportedFromGridWh);
            cmd.Parameters.AddWithValue("@StoredInBatteriesWh", interval.StoredInBatteriesWh);
            cmd.Parameters.AddWithValue("@DischargedFromBatteriesWh", interval.DischargedFromBatteriesWh);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<SolarVendorInterval> GetSolarVendorIntervals(DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, Timestamp, EnergyProducedWh, EnergyConsumedWh,
                       ExportedToGridWh, ImportedFromGridWh,
                       StoredInBatteriesWh, DischargedFromBatteriesWh
                FROM SolarVendorIntervals
                WHERE Timestamp >= @Start AND Timestamp <= @End
                ORDER BY Timestamp;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<SolarVendorInterval>();

            while (reader.Read())
            {
                list.Add(new SolarVendorInterval
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Timestamp = reader.GetDateTime(1),
                    EnergyProducedWh = reader.GetInt32(2),
                    EnergyConsumedWh = reader.GetInt32(3),
                    ExportedToGridWh = reader.GetInt32(4),
                    ImportedFromGridWh = reader.GetInt32(5),
                    StoredInBatteriesWh = reader.GetInt32(6),
                    DischargedFromBatteriesWh = reader.GetInt32(7)
                });
            }

            return list;
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING SESSIONS
        // ---------------------------------------------------------

        public void AddEvCommercialChargingSession(EvCommercialChargingSession session)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO EvCommercialChargingSessions
                (Id, StartTime, EndTime, KwhAdded, ChargingCost, VendorSessionId, Location)
                VALUES
                (@Id, @StartTime, @EndTime, @KwhAdded, @ChargingCost, @VendorSessionId, @Location);", conn);

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@EndTime", session.EndTime);
            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@ChargingCost", session.ChargingCost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorSessionId", session.VendorSessionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Location", session.Location ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<EvCommercialChargingSession> GetEvCommercialChargingSessions(
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, StartTime, EndTime, KwhAdded, ChargingCost, VendorSessionId, Location
                FROM EvCommercialChargingSessions
                WHERE StartTime >= @Start AND EndTime <= @End
                ORDER BY StartTime;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<EvCommercialChargingSession>();

            while (reader.Read())
            {
                list.Add(new EvCommercialChargingSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartTime = reader.GetDateTime(1),
                    EndTime = reader.GetDateTime(2),
                    KwhAdded = reader.GetDecimal(3),
                    ChargingCost = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    VendorSessionId = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Location = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return list;
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS
        // ---------------------------------------------------------

        public void AddCommercialChargingCostRecord(CommercialChargingCostRecord record)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO CommercialChargingCostRecords
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

        public IEnumerable<CommercialChargingCostRecord> GetCommercialChargingCostRecords(
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, SessionId, Date, Cost, Vendor
                FROM CommercialChargingCostRecords
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<CommercialChargingCostRecord>();

            while (reader.Read())
            {
                list.Add(new CommercialChargingCostRecord
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

        public IEnumerable<CommercialChargingCostRecord> GetCommercialChargingCostRecordsByVendor(
            string vendor,
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, SessionId, Date, Cost, Vendor
                FROM CommercialChargingCostRecords
                WHERE Vendor = @Vendor
                AND Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@Vendor", vendor);
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<CommercialChargingCostRecord>();

            while (reader.Read())
            {
                list.Add(new CommercialChargingCostRecord
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
    }
}
