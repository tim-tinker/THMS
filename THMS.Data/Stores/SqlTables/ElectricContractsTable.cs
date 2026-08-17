using Microsoft.Data.Sqlite;
using THMS.Domain.Finance;

namespace THMS.Data.Stores.SqlTables
{
    public class ElectricContractsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ElectricContracts (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    BaseEnergyCharge REAL NOT NULL,
                    EnergyChargeRate REAL NOT NULL,
                    BaseDeliveryCharge REAL NOT NULL,
                    DeliveryChargeRate REAL NOT NULL,
                    ExportCreditRate REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();

            using var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE ElectricContracts ADD COLUMN IF NOT EXISTS Name TEXT;";
            alter.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, ElectricContract contract)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO ElectricContracts
                (Id, Name, StartDate, EndDate, BaseEnergyCharge, EnergyChargeRate,
                 BaseDeliveryCharge, DeliveryChargeRate, ExportCreditRate)
                VALUES
                (@Id, @Name, @StartDate, @EndDate, @BaseEnergyCharge, @EnergyChargeRate,
                 @BaseDeliveryCharge, @DeliveryChargeRate, @ExportCreditRate)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = excluded.Name,
                    StartDate = excluded.StartDate,
                    EndDate = excluded.EndDate,
                    BaseEnergyCharge = excluded.BaseEnergyCharge,
                    EnergyChargeRate = excluded.EnergyChargeRate,
                    BaseDeliveryCharge = excluded.BaseDeliveryCharge,
                    DeliveryChargeRate = excluded.DeliveryChargeRate,
                    ExportCreditRate = excluded.ExportCreditRate;", conn);

            Bind(cmd, contract);
            cmd.ExecuteNonQuery();
        }

        public ElectricContract? GetById(SqliteConnection conn, Guid contractId)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, Name, StartDate, EndDate,
                       BaseEnergyCharge, EnergyChargeRate,
                       BaseDeliveryCharge, DeliveryChargeRate, ExportCreditRate
                FROM ElectricContracts
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", contractId.ToString());

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public ElectricContract? GetForDate(SqliteConnection conn, DateTime date)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, Name, StartDate, EndDate,
                       BaseEnergyCharge, EnergyChargeRate,
                       BaseDeliveryCharge, DeliveryChargeRate, ExportCreditRate
                FROM ElectricContracts
                WHERE StartDate <= @Date AND EndDate >= @Date
                ORDER BY EndDate DESC
                LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("@Date", date);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<ElectricContract> GetRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, Name, StartDate, EndDate,
                       BaseEnergyCharge, EnergyChargeRate,
                       BaseDeliveryCharge, DeliveryChargeRate, ExportCreditRate
                FROM ElectricContracts
                WHERE StartDate <= @End AND EndDate >= @Start
                ORDER BY StartDate;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<ElectricContract>();

            while (reader.Read())
                list.Add(Read(reader));

            return list;
        }

        public ElectricContract? GetLatest(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, Name, StartDate, EndDate,
                       BaseEnergyCharge, EnergyChargeRate,
                       BaseDeliveryCharge, DeliveryChargeRate, ExportCreditRate
                FROM ElectricContracts
                ORDER BY StartDate DESC
                LIMIT 1;", conn);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        private static void Bind(SqliteCommand cmd, ElectricContract contract)
        {
            cmd.Parameters.AddWithValue("@Id", contract.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", (object?)contract.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StartDate", contract.StartDate);
            cmd.Parameters.AddWithValue("@EndDate", contract.EndDate);
            cmd.Parameters.AddWithValue("@BaseEnergyCharge", contract.BaseEnergyCharge);
            cmd.Parameters.AddWithValue("@EnergyChargeRate", contract.EnergyChargeRate);
            cmd.Parameters.AddWithValue("@BaseDeliveryCharge", contract.BaseDeliveryCharge);
            cmd.Parameters.AddWithValue("@DeliveryChargeRate", contract.DeliveryChargeRate);
            cmd.Parameters.AddWithValue("@ExportCreditRate", contract.ExportCreditRate);
        }

        private static ElectricContract Read(SqliteDataReader reader)
        {
            return new ElectricContract
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                StartDate = reader.GetDateTime(2),
                EndDate = reader.GetDateTime(3),
                BaseEnergyCharge = (decimal)(double)reader.GetDouble(4),
                EnergyChargeRate = (decimal)(double)reader.GetDouble(5),
                BaseDeliveryCharge = (decimal)(double)reader.GetDouble(6),
                DeliveryChargeRate = (decimal)(double)reader.GetDouble(7),
                ExportCreditRate = (decimal)(double)reader.GetDouble(8)
            };
        }
    }
}
