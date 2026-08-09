using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class GasPurchasesTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS GasPurchases (
                    Id TEXT PRIMARY KEY,
                    VehicleId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Gallons REAL NOT NULL,
                    FuelCost REAL NOT NULL,
                    Station TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, GasPurchase purchase)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO GasPurchases
                (Id, VehicleId, Date, Gallons, FuelCost, Station)
                VALUES
                (@Id, @VehicleId, @Date, @Gallons, @FuelCost, @Station)
                ON CONFLICT(Id) DO UPDATE SET
                    VehicleId = excluded.VehicleId,
                    Date = excluded.Date,
                    Gallons = excluded.Gallons,
                    FuelCost = excluded.FuelCost,
                    Station = excluded.Station;", conn);

            cmd.Parameters.AddWithValue("@Id", purchase.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", purchase.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@Date", purchase.Date);
            cmd.Parameters.AddWithValue("@Gallons", purchase.Gallons);
            cmd.Parameters.AddWithValue("@FuelCost", purchase.FuelCost);
            cmd.Parameters.AddWithValue("@Station", purchase.Station ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<GasPurchase> GetRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, VehicleId, Date, Gallons, FuelCost, Station
                FROM GasPurchases
                WHERE VehicleId = @VehicleId
                AND Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            return ReadAll(cmd);
        }

        public IEnumerable<GasPurchase> GetWithMissingCost(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, VehicleId, Date, Gallons, FuelCost, Station
                FROM GasPurchases
                WHERE FuelCost = 0
                ORDER BY Date;", conn);

            return ReadAll(cmd);
        }

        public void UpdateCost(SqliteConnection conn, Guid purchaseId, decimal cost)
        {
            using var cmd = new SqliteCommand(@"
                UPDATE GasPurchases
                SET FuelCost = @Cost
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", purchaseId.ToString());
            cmd.Parameters.AddWithValue("@Cost", cost);
            cmd.ExecuteNonQuery();
        }

        private static IEnumerable<GasPurchase> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<GasPurchase>();

            while (reader.Read())
            {
                list.Add(new GasPurchase
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    VehicleId = Guid.Parse(reader.GetString(1)),
                    Date = reader.GetDateTime(2),
                    Gallons = reader.GetDecimal(3),
                    FuelCost = reader.GetDecimal(4),
                    Station = reader.IsDBNull(5) ? null! : reader.GetString(5)
                });
            }

            return list;
        }
    }
}
