using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class Retirement401kLimitsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Retirement401kLimits (
                    TaxYear INTEGER PRIMARY KEY,
                    ElectiveDeferral REAL NOT NULL,
                    CatchUp REAL NOT NULL,
                    HigherCatchUp REAL NOT NULL,
                    RothCatchUpWageThreshold REAL NOT NULL,
                    AnnualAdditions REAL NOT NULL,
                    CompensationCap REAL NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Retirement401kLimits limits)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Retirement401kLimits (
                    TaxYear, ElectiveDeferral, CatchUp, HigherCatchUp,
                    RothCatchUpWageThreshold, AnnualAdditions, CompensationCap, ModifiedOn)
                VALUES (
                    @TaxYear, @ElectiveDeferral, @CatchUp, @HigherCatchUp,
                    @RothCatchUpWageThreshold, @AnnualAdditions, @CompensationCap, @ModifiedOn)
                ON CONFLICT(TaxYear) DO UPDATE SET
                    ElectiveDeferral = excluded.ElectiveDeferral,
                    CatchUp = excluded.CatchUp,
                    HigherCatchUp = excluded.HigherCatchUp,
                    RothCatchUpWageThreshold = excluded.RothCatchUpWageThreshold,
                    AnnualAdditions = excluded.AnnualAdditions,
                    CompensationCap = excluded.CompensationCap,
                    ModifiedOn = excluded.ModifiedOn;";
            cmd.Parameters.AddWithValue("@TaxYear", limits.TaxYear);
            cmd.Parameters.AddWithValue("@ElectiveDeferral", limits.ElectiveDeferral);
            cmd.Parameters.AddWithValue("@CatchUp", limits.CatchUp);
            cmd.Parameters.AddWithValue("@HigherCatchUp", limits.HigherCatchUp);
            cmd.Parameters.AddWithValue("@RothCatchUpWageThreshold", limits.RothCatchUpWageThreshold);
            cmd.Parameters.AddWithValue("@AnnualAdditions", limits.AnnualAdditions);
            cmd.Parameters.AddWithValue("@CompensationCap", limits.CompensationCap);
            cmd.Parameters.AddWithValue("@ModifiedOn", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public Retirement401kLimits? Get(SqliteConnection conn, int taxYear)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TaxYear, ElectiveDeferral, CatchUp, HigherCatchUp,
                    RothCatchUpWageThreshold, AnnualAdditions, CompensationCap
                FROM Retirement401kLimits
                WHERE TaxYear = @TaxYear;";
            cmd.Parameters.AddWithValue("@TaxYear", taxYear);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public Retirement401kLimits? GetLatest(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TaxYear, ElectiveDeferral, CatchUp, HigherCatchUp,
                    RothCatchUpWageThreshold, AnnualAdditions, CompensationCap
                FROM Retirement401kLimits
                ORDER BY TaxYear DESC
                LIMIT 1;";
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        private static Retirement401kLimits Read(SqliteDataReader reader) => new()
        {
            TaxYear = reader.GetInt32(0),
            ElectiveDeferral = Money(reader, 1),
            CatchUp = Money(reader, 2),
            HigherCatchUp = Money(reader, 3),
            RothCatchUpWageThreshold = Money(reader, 4),
            AnnualAdditions = Money(reader, 5),
            CompensationCap = Money(reader, 6)
        };

        private static decimal Money(SqliteDataReader reader, int ordinal) =>
            (decimal)(double)reader.GetDouble(ordinal);
    }
}
