using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class GasPurchaseTable
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

        public void Insert(SqliteConnection conn, GasPurchase purchase)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO GasPurchases
                (Id, VehicleId, Date, Gallons, FuelCost, Station)
                VALUES
                (@Id, @VehicleId, @Date, @Gallons, @FuelCost, @Station);";

            cmd.Parameters.AddWithValue("@Id", purchase.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", purchase.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@Date", purchase.Date);
            cmd.Parameters.AddWithValue("@Gallons", purchase.Gallons);
            cmd.Parameters.AddWithValue("@FuelCost", purchase.FuelCost);
            cmd.Parameters.AddWithValue("@Station", (object?)purchase.Station ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<GasPurchase> GetRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, VehicleId, Date, Gallons, FuelCost, Station
                FROM GasPurchases
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new GasPurchase
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    VehicleId = Guid.Parse(reader.GetString(1)),
                    Date = reader.GetDateTime(2),
                    Gallons = (decimal)(double)reader.GetDouble(3),
                    FuelCost = (decimal)(double)reader.GetDouble(4),
                    Station = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                };
            }
        }

        public decimal GetTotalFuelCost(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT SUM(FuelCost)
                FROM GasPurchases
                WHERE Date >= @Start AND Date <= @End;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            var result = cmd.ExecuteScalar();
            return result is null or DBNull ? 0m : (decimal)(double)result;
        }
    }
}
