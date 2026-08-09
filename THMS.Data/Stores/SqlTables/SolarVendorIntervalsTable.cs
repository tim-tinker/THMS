using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using THMS.Domain.Energy;

namespace THMS.Data.Stores.SqlTables
{
    public class SolarVendorIntervalsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS SolarVendorIntervals (
                Id TEXT PRIMARY KEY,
                Timestamp TEXT NOT NULL,
                EnergyProducedWh INTEGER NOT NULL,
                EnergyConsumedWh INTEGER NOT NULL,
                ExportedToGridWh INTEGER NOT NULL,
                ImportedFromGridWh INTEGER NOT NULL,
                StoredInBatteriesWh INTEGER NOT NULL,
                DischargedFromBatteriesWh INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_SolarVendorIntervals_Timestamp
                ON SolarVendorIntervals (Timestamp);
            ";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, SolarVendorInterval interval)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO SolarVendorIntervals
                (Id, Timestamp, EnergyProducedWh, EnergyConsumedWh,
                 ExportedToGridWh, ImportedFromGridWh,
                 StoredInBatteriesWh, DischargedFromBatteriesWh)
                VALUES
                (@Id, @Timestamp, @EnergyProducedWh, @EnergyConsumedWh,
                 @ExportedToGridWh, @ImportedFromGridWh,
                 @StoredInBatteriesWh, @DischargedFromBatteriesWh)
                ON CONFLICT(Timestamp) DO UPDATE SET
                    EnergyProducedWh = excluded.EnergyProducedWh,
                    EnergyConsumedWh = excluded.EnergyConsumedWh,
                    ExportedToGridWh = excluded.ExportedToGridWh,
                    ImportedFromGridWh = excluded.ImportedFromGridWh,
                    StoredInBatteriesWh = excluded.StoredInBatteriesWh,
                    DischargedFromBatteriesWh = excluded.DischargedFromBatteriesWh;", conn);

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

        public IEnumerable<SolarVendorInterval> GetRange(SqliteConnection conn, DateTime start, DateTime end)
        {
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

            while (reader.Read())
                yield return Read(reader);
        }

        public SolarVendorInterval? GetLatest(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, Timestamp, EnergyProducedWh, EnergyConsumedWh,
                       ExportedToGridWh, ImportedFromGridWh,
                       StoredInBatteriesWh, DischargedFromBatteriesWh
                FROM SolarVendorIntervals
                ORDER BY Timestamp DESC
                LIMIT 1;", conn);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        private static SolarVendorInterval Read(SqliteDataReader reader)
        {
            return new SolarVendorInterval
            {
                Id = Guid.Parse(reader.GetString(0)),
                Timestamp = reader.GetDateTime(1),
                EnergyProducedWh = reader.GetInt32(2),
                EnergyConsumedWh = reader.GetInt32(3),
                ExportedToGridWh = reader.GetInt32(4),
                ImportedFromGridWh = reader.GetInt32(5),
                StoredInBatteriesWh = reader.GetInt32(6),
                DischargedFromBatteriesWh = reader.GetInt32(7)
            };
        }
    }
}
