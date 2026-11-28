using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Income;

namespace THMS.Data.Stores.SqlTables
{
    public class PayslipsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Payslips (
                    Id TEXT PRIMARY KEY,
                    PayDate TEXT NOT NULL,
                    Employer TEXT NOT NULL DEFAULT '',
                    Gross REAL NOT NULL,
                    GrossYearToDate REAL NOT NULL,
                    Traditional401k REAL NOT NULL,
                    Traditional401kYearToDate REAL NOT NULL,
                    Roth401k REAL NOT NULL,
                    Roth401kYearToDate REAL NOT NULL,
                    Hsa REAL NOT NULL,
                    HsaYearToDate REAL NOT NULL,
                    SocialSecurityTax REAL NOT NULL,
                    SocialSecurityTaxYearToDate REAL NOT NULL,
                    MedicareTax REAL NOT NULL,
                    MedicareTaxYearToDate REAL NOT NULL,
                    FederalIncomeTax REAL NOT NULL,
                    FederalIncomeTaxYearToDate REAL NOT NULL,
                    GroupTermLifeTaxable REAL NOT NULL,
                    GroupTermLifeTaxableYearToDate REAL NOT NULL,
                    CreatedOn TEXT NOT NULL,
                    ModifiedOn TEXT NOT NULL,
                    PersonId TEXT NOT NULL DEFAULT ''
                );";
            cmd.ExecuteNonQuery();
            foreach (var column in new[]
            {
                "SocialSecurityTax",
                "SocialSecurityTaxYearToDate",
                "MedicareTax",
                "MedicareTaxYearToDate",
                "FederalIncomeTax",
                "FederalIncomeTaxYearToDate",
                "GroupTermLifeTaxable",
                "GroupTermLifeTaxableYearToDate"
            })
                SqliteSchema.EnsureColumn(conn, "Payslips", column, "REAL NOT NULL DEFAULT 0");
            SqliteSchema.EnsureColumn(conn, "Payslips", "Employer", "TEXT NOT NULL DEFAULT ''");
            SqliteSchema.EnsureColumn(conn, "Payslips", "PersonId", "TEXT NOT NULL DEFAULT ''");
            DropColumnIfExists(conn, "SocialSecurityWages");
            DropColumnIfExists(conn, "SocialSecurityWagesYearToDate");
            DropColumnIfExists(conn, "EmployerMatch");
            DropColumnIfExists(conn, "EmployerMatchYearToDate");
        }

        private static void DropColumnIfExists(SqliteConnection conn, string columnName)
        {
            using var info = conn.CreateCommand();
            info.CommandText = "PRAGMA table_info(Payslips);";
            var exists = false;
            using (var reader = info.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
                return;

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE Payslips DROP COLUMN {columnName};";
            alter.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Payslip payslip)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Payslips (
                    Id, PayDate, Employer, Gross, GrossYearToDate,
                    Traditional401k, Traditional401kYearToDate,
                    Roth401k, Roth401kYearToDate,
                    Hsa, HsaYearToDate,
                    SocialSecurityTax, SocialSecurityTaxYearToDate,
                    MedicareTax, MedicareTaxYearToDate,
                    FederalIncomeTax, FederalIncomeTaxYearToDate,
                    GroupTermLifeTaxable, GroupTermLifeTaxableYearToDate,
                    CreatedOn, ModifiedOn, PersonId)
                VALUES (
                    @Id, @PayDate, @Employer, @Gross, @GrossYearToDate,
                    @Traditional401k, @Traditional401kYearToDate,
                    @Roth401k, @Roth401kYearToDate,
                    @Hsa, @HsaYearToDate,
                    @SocialSecurityTax, @SocialSecurityTaxYearToDate,
                    @MedicareTax, @MedicareTaxYearToDate,
                    @FederalIncomeTax, @FederalIncomeTaxYearToDate,
                    @GroupTermLifeTaxable, @GroupTermLifeTaxableYearToDate,
                    @CreatedOn, @ModifiedOn, @PersonId)
                ON CONFLICT(Id) DO UPDATE SET
                    PayDate = excluded.PayDate,
                    Employer = excluded.Employer,
                    Gross = excluded.Gross,
                    GrossYearToDate = excluded.GrossYearToDate,
                    Traditional401k = excluded.Traditional401k,
                    Traditional401kYearToDate = excluded.Traditional401kYearToDate,
                    Roth401k = excluded.Roth401k,
                    Roth401kYearToDate = excluded.Roth401kYearToDate,
                    Hsa = excluded.Hsa,
                    HsaYearToDate = excluded.HsaYearToDate,
                    SocialSecurityTax = excluded.SocialSecurityTax,
                    SocialSecurityTaxYearToDate = excluded.SocialSecurityTaxYearToDate,
                    MedicareTax = excluded.MedicareTax,
                    MedicareTaxYearToDate = excluded.MedicareTaxYearToDate,
                    FederalIncomeTax = excluded.FederalIncomeTax,
                    FederalIncomeTaxYearToDate = excluded.FederalIncomeTaxYearToDate,
                    GroupTermLifeTaxable = excluded.GroupTermLifeTaxable,
                    GroupTermLifeTaxableYearToDate = excluded.GroupTermLifeTaxableYearToDate,
                    ModifiedOn = excluded.ModifiedOn,
                    PersonId = excluded.PersonId;";
            Bind(cmd, payslip);
            cmd.ExecuteNonQuery();
        }

        public List<Payslip> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, PayDate, Employer, Gross, GrossYearToDate,
                    Traditional401k, Traditional401kYearToDate,
                    Roth401k, Roth401kYearToDate,
                    Hsa, HsaYearToDate,
                    SocialSecurityTax, SocialSecurityTaxYearToDate,
                    MedicareTax, MedicareTaxYearToDate,
                    FederalIncomeTax, FederalIncomeTaxYearToDate,
                    GroupTermLifeTaxable, GroupTermLifeTaxableYearToDate,
                    CreatedOn, ModifiedOn, PersonId
                FROM Payslips
                ORDER BY PayDate DESC, CreatedOn DESC;";
            var payslips = new List<Payslip>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                payslips.Add(Read(reader));
            return payslips;
        }

        public Payslip? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, PayDate, Employer, Gross, GrossYearToDate,
                    Traditional401k, Traditional401kYearToDate,
                    Roth401k, Roth401kYearToDate,
                    Hsa, HsaYearToDate,
                    SocialSecurityTax, SocialSecurityTaxYearToDate,
                    MedicareTax, MedicareTaxYearToDate,
                    FederalIncomeTax, FederalIncomeTaxYearToDate,
                    GroupTermLifeTaxable, GroupTermLifeTaxableYearToDate,
                    CreatedOn, ModifiedOn, PersonId
                FROM Payslips
                WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Payslips WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void Bind(SqliteCommand cmd, Payslip payslip)
        {
            cmd.Parameters.AddWithValue("@Id", payslip.Id.ToString());
            cmd.Parameters.AddWithValue("@PayDate", payslip.PayDate);
            cmd.Parameters.AddWithValue("@Employer", payslip.Employer ?? "");
            cmd.Parameters.AddWithValue("@Gross", payslip.Gross);
            cmd.Parameters.AddWithValue("@GrossYearToDate", payslip.GrossYearToDate);
            cmd.Parameters.AddWithValue("@Traditional401k", payslip.Traditional401k);
            cmd.Parameters.AddWithValue("@Traditional401kYearToDate", payslip.Traditional401kYearToDate);
            cmd.Parameters.AddWithValue("@Roth401k", payslip.Roth401k);
            cmd.Parameters.AddWithValue("@Roth401kYearToDate", payslip.Roth401kYearToDate);
            cmd.Parameters.AddWithValue("@Hsa", payslip.Hsa);
            cmd.Parameters.AddWithValue("@HsaYearToDate", payslip.HsaYearToDate);
            cmd.Parameters.AddWithValue("@SocialSecurityTax", payslip.SocialSecurityTax);
            cmd.Parameters.AddWithValue("@SocialSecurityTaxYearToDate", payslip.SocialSecurityTaxYearToDate);
            cmd.Parameters.AddWithValue("@MedicareTax", payslip.MedicareTax);
            cmd.Parameters.AddWithValue("@MedicareTaxYearToDate", payslip.MedicareTaxYearToDate);
            cmd.Parameters.AddWithValue("@FederalIncomeTax", payslip.FederalIncomeTax);
            cmd.Parameters.AddWithValue("@FederalIncomeTaxYearToDate", payslip.FederalIncomeTaxYearToDate);
            cmd.Parameters.AddWithValue("@GroupTermLifeTaxable", payslip.GroupTermLifeTaxable);
            cmd.Parameters.AddWithValue("@GroupTermLifeTaxableYearToDate", payslip.GroupTermLifeTaxableYearToDate);
            cmd.Parameters.AddWithValue("@CreatedOn", payslip.CreatedOn);
            cmd.Parameters.AddWithValue("@ModifiedOn", payslip.ModifiedOn);
            cmd.Parameters.AddWithValue("@PersonId", payslip.PersonId == Guid.Empty ? "" : payslip.PersonId.ToString());
        }

        private static Payslip Read(SqliteDataReader reader) => new()
        {
            Id = Guid.Parse(reader.GetString(0)),
            PayDate = reader.GetDateTime(1),
            Employer = reader.GetString(2),
            Gross = Money(reader, 3),
            GrossYearToDate = Money(reader, 4),
            Traditional401k = Money(reader, 5),
            Traditional401kYearToDate = Money(reader, 6),
            Roth401k = Money(reader, 7),
            Roth401kYearToDate = Money(reader, 8),
            Hsa = Money(reader, 9),
            HsaYearToDate = Money(reader, 10),
            SocialSecurityTax = Money(reader, 11),
            SocialSecurityTaxYearToDate = Money(reader, 12),
            MedicareTax = Money(reader, 13),
            MedicareTaxYearToDate = Money(reader, 14),
            FederalIncomeTax = Money(reader, 15),
            FederalIncomeTaxYearToDate = Money(reader, 16),
            GroupTermLifeTaxable = Money(reader, 17),
            GroupTermLifeTaxableYearToDate = Money(reader, 18),
            CreatedOn = reader.GetDateTime(19),
            ModifiedOn = reader.GetDateTime(20),
            PersonId = ReadPerson(reader, 21)
        };

        private static decimal Money(SqliteDataReader reader, int ordinal) =>
            (decimal)(double)reader.GetDouble(ordinal);

        private static Guid ReadPerson(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return Guid.Empty;
            var text = reader.GetString(ordinal);
            return Guid.TryParse(text, out var id) ? id : Guid.Empty;
        }
    }
}
