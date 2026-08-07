using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using THMS.Domain.Energy;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteFinanceDataStore : IFinanceDataStore
    {
        private readonly string _connectionString;

        public SQLiteFinanceDataStore(string databasePath)
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
        // ELECTRIC UTILITY BILLS
        // ---------------------------------------------------------

        public void AddElectricUtilityBill(ElectricUtilityBill bill)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO ElectricUtilityBills
                (Id, StartDate, EndDate, GridImportCost, GridExportCredit,
                 DeliveryCharges, FixedCharges, TaxesAndFees, TotalKwh)
                VALUES
                (@Id, @StartDate, @EndDate, @GridImportCost, @GridExportCredit,
                 @DeliveryCharges, @FixedCharges, @TaxesAndFees, @TotalKwh);", conn);

            cmd.Parameters.AddWithValue("@Id", bill.Id.ToString());
            cmd.Parameters.AddWithValue("@StartDate", bill.StartDate);
            cmd.Parameters.AddWithValue("@EndDate", bill.EndDate);
            cmd.Parameters.AddWithValue("@GridImportCost", bill.GridImportCost);
            cmd.Parameters.AddWithValue("@GridExportCredit", bill.GridExportCredit);
            cmd.Parameters.AddWithValue("@DeliveryCharges", bill.DeliveryCharges);
            cmd.Parameters.AddWithValue("@FixedCharges", bill.FixedCharges);
            cmd.Parameters.AddWithValue("@TaxesAndFees", bill.TaxesAndFees);
            cmd.Parameters.AddWithValue("@TotalKwh", bill.TotalKwh);


            cmd.ExecuteNonQuery();
        }

        public IEnumerable<ElectricUtilityBill> GetElectricUtilityBills(
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, StartDate, EndDate,
                       GridImportCost, GridExportCredit,
                       DeliveryCharges, FixedCharges, TaxesAndFees,
                       TotalKwh
                FROM ElectricUtilityBills
                WHERE StartDate >= @Start AND EndDate <= @End
                ORDER BY StartDate;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<ElectricUtilityBill>();

            while (reader.Read())
            {
                list.Add(new ElectricUtilityBill
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartDate = reader.GetDateTime(1),
                    EndDate = reader.GetDateTime(2),
                    GridImportCost = reader.GetDecimal(3),
                    GridExportCredit = reader.GetDecimal(4),
                    DeliveryCharges = reader.GetDecimal(5),
                    FixedCharges = reader.GetDecimal(6),
                    TaxesAndFees = reader.GetDecimal(7),
                    TotalKwh = reader.GetDecimal(8)
                }
);
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
                (Id, SessionId, Date, Cost)
                VALUES
                (@Id, @SessionId, @Date, @Cost);", conn);

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@SessionId", record.SessionId.ToString());
            cmd.Parameters.AddWithValue("@Date", record.Date);
            cmd.Parameters.AddWithValue("@Cost", record.Cost);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecords(
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, SessionId, Date, Cost
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
                    Cost = reader.GetDecimal(3)
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

        // ---------------------------------------------------------
        // GAS PURCHASES (ICE VEHICLES)
        // ---------------------------------------------------------

        public void AddGasPurchase(GasPurchase purchase)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO GasPurchases
                (Id, VehicleId, Date, Gallons, FuelCost, Station)
                VALUES
                (@Id, @VehicleId, @Date, @Gallons, @FuelCost, @Station);", conn);

            cmd.Parameters.AddWithValue("@Id", purchase.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", purchase.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@Date", purchase.Date);
            cmd.Parameters.AddWithValue("@Gallons", purchase.Gallons);
            cmd.Parameters.AddWithValue("@FuelCost", purchase.FuelCost);
            cmd.Parameters.AddWithValue("@Station", purchase.Station ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<GasPurchase> GetGasPurchases(
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, VehicleId, Date, Gallons, FuelCost, Station
                FROM GasPurchases
                WHERE VehicleId = @VehicleId
                AND Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

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
                    Station = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return list;
        }

        // ---------------------------------------------------------
        // INCOMPLETE COST RECORDS
        // ---------------------------------------------------------

        public IEnumerable<EvChargeSession> GetEvChargeSessionsWithMissingCost()
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, StartTime, EndTime, KwhAdded,
                       IsHomeCharge, ChargeCost, VehicleDataId
                FROM EvChargeSessions
                WHERE ChargeCost IS NULL
                ORDER BY StartTime;", conn);

            using var reader = cmd.ExecuteReader();
            var list = new List<EvChargeSession>();

            while (reader.Read())
            {
                list.Add(new EvChargeSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartTime = reader.GetDateTime(1),
                    EndTime = reader.GetDateTime(2),
                    KwhAdded = reader.GetDecimal(3),
                    IsHomeCharge = reader.GetBoolean(4),
                    SessionCost = 0,
                });
            }

            return list;
        }

        public IEnumerable<GasPurchase> GetGasPurchasesWithMissingCost()
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, VehicleId, Date, Gallons, FuelCost, Station
                FROM GasPurchases
                WHERE FuelCost IS NULL
                ORDER BY Date;", conn);

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
                    FuelCost = 0, // cost is null, user must update
                    Station = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return list;
        }

        // ---------------------------------------------------------
        // COST UPDATES
        // ---------------------------------------------------------

        public void UpdateEvChargeSessionCost(Guid sessionId, decimal cost)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                UPDATE EvChargeSessions
                SET ChargeCost = @Cost
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());
            cmd.Parameters.AddWithValue("@Cost", cost);

            cmd.ExecuteNonQuery();
        }

        public void UpdateGasPurchaseCost(Guid purchaseId, decimal cost)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                UPDATE GasPurchases
                SET FuelCost = @Cost
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", purchaseId.ToString());
            cmd.Parameters.AddWithValue("@Cost", cost);

            cmd.ExecuteNonQuery();
        }
    }
}
