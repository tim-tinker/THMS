using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class Retirement401kMatchTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Retirement401kMatch (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    FirstMatchRate REAL NOT NULL,
                    FirstMatchOfPay REAL NOT NULL,
                    NextMatchRate REAL NOT NULL,
                    NextMatchOfPay REAL NOT NULL,
                    PlanOffersRoth INTEGER NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Retirement401kMatchFormula formula)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Retirement401kMatch (
                    Id, FirstMatchRate, FirstMatchOfPay, NextMatchRate, NextMatchOfPay,
                    PlanOffersRoth, ModifiedOn)
                VALUES (
                    1, @FirstMatchRate, @FirstMatchOfPay, @NextMatchRate, @NextMatchOfPay,
                    @PlanOffersRoth, @ModifiedOn)
                ON CONFLICT(Id) DO UPDATE SET
                    FirstMatchRate = excluded.FirstMatchRate,
                    FirstMatchOfPay = excluded.FirstMatchOfPay,
                    NextMatchRate = excluded.NextMatchRate,
                    NextMatchOfPay = excluded.NextMatchOfPay,
                    PlanOffersRoth = excluded.PlanOffersRoth,
                    ModifiedOn = excluded.ModifiedOn;";
            cmd.Parameters.AddWithValue("@FirstMatchRate", formula.FirstMatchRate);
            cmd.Parameters.AddWithValue("@FirstMatchOfPay", formula.FirstMatchOfPay);
            cmd.Parameters.AddWithValue("@NextMatchRate", formula.NextMatchRate);
            cmd.Parameters.AddWithValue("@NextMatchOfPay", formula.NextMatchOfPay);
            cmd.Parameters.AddWithValue("@PlanOffersRoth", formula.PlanOffersRoth ? 1 : 0);
            cmd.Parameters.AddWithValue("@ModifiedOn", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public Retirement401kMatchFormula? Get(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT FirstMatchRate, FirstMatchOfPay, NextMatchRate, NextMatchOfPay, PlanOffersRoth
                FROM Retirement401kMatch
                WHERE Id = 1;";
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;
            return new Retirement401kMatchFormula
            {
                FirstMatchRate = Rate(reader, 0),
                FirstMatchOfPay = Rate(reader, 1),
                NextMatchRate = Rate(reader, 2),
                NextMatchOfPay = Rate(reader, 3),
                PlanOffersRoth = reader.GetInt32(4) != 0
            };
        }

        private static decimal Rate(SqliteDataReader reader, int ordinal) =>
            decimal.Round((decimal)(double)reader.GetDouble(ordinal), 4, MidpointRounding.AwayFromZero);
    }
}
