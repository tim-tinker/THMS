using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using THMS.Domain.Energy;

namespace THMS.Data.Stores.SqqlTables
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
            ";
            cmd.ExecuteNonQuery();
        }

        public void Insert(SqliteConnection conn, SolarVendorInterval interval)
        {
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
            var list = new List<SolarVendorInterval>();

            while (reader.Read())
            {
                yield return new SolarVendorInterval
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
}
