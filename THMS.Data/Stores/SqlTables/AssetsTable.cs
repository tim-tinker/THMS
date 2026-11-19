using Microsoft.Data.Sqlite;
using THMS.Domain.Assets;

namespace THMS.Data.Stores.SqlTables
{
    public class AssetsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Assets (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    AcquiredOn TEXT,
                    CostBasis REAL NOT NULL,
                    HowAcquired TEXT NOT NULL,
                    AcquisitionNotes TEXT NOT NULL,
                    ClassType TEXT NOT NULL,
                    CreatedOn TEXT NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS PropertyAssets (
                    AssetId TEXT PRIMARY KEY,
                    ParcelAddress TEXT NOT NULL,
                    CadNumber TEXT NOT NULL,
                    TaxAccountNumber TEXT NOT NULL,
                    LegalDescription TEXT NOT NULL,
                    LegalAcres REAL NOT NULL,
                    RollCode TEXT NOT NULL,
                    AssessmentMethod TEXT NOT NULL DEFAULT 'ApplyHomesteadCap',
                    LevyMethod TEXT NOT NULL DEFAULT 'RatePerHundred'
                );
                CREATE TABLE IF NOT EXISTS VehicleAssets (
                    AssetId TEXT PRIMARY KEY,
                    VehicleId TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
            SqliteSchema.EnsureColumn(conn, "PropertyAssets", "AssessmentMethod", "TEXT NOT NULL DEFAULT 'ApplyHomesteadCap'");
            SqliteSchema.EnsureColumn(conn, "PropertyAssets", "LevyMethod", "TEXT NOT NULL DEFAULT 'RatePerHundred'");
        }

        public void Upsert(SqliteConnection conn, Asset asset)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Assets
                        (Id, Name, Category, AcquiredOn, CostBasis, HowAcquired, AcquisitionNotes,
                         ClassType, CreatedOn, ModifiedOn)
                    VALUES
                        (@Id, @Name, @Category, @AcquiredOn, @CostBasis, @HowAcquired, @AcquisitionNotes,
                         @ClassType, @CreatedOn, @ModifiedOn)
                    ON CONFLICT(Id) DO UPDATE SET
                        Name = excluded.Name,
                        Category = excluded.Category,
                        AcquiredOn = excluded.AcquiredOn,
                        CostBasis = excluded.CostBasis,
                        HowAcquired = excluded.HowAcquired,
                        AcquisitionNotes = excluded.AcquisitionNotes,
                        ClassType = excluded.ClassType,
                        ModifiedOn = excluded.ModifiedOn;";
                cmd.Parameters.AddWithValue("@Id", asset.Id.ToString());
                cmd.Parameters.AddWithValue("@Name", asset.Name);
                cmd.Parameters.AddWithValue("@Category", asset.Category ?? "");
                cmd.Parameters.AddWithValue("@AcquiredOn", asset.AcquiredOn is DateTime acquired
                    ? acquired
                    : DBNull.Value);
                cmd.Parameters.AddWithValue("@CostBasis", asset.CostBasis);
                cmd.Parameters.AddWithValue("@HowAcquired", asset.HowAcquired ?? "");
                cmd.Parameters.AddWithValue("@AcquisitionNotes", asset.AcquisitionNotes ?? "");
                cmd.Parameters.AddWithValue("@ClassType", asset.GetType().Name);
                cmd.Parameters.AddWithValue("@CreatedOn", asset.CreatedOn);
                cmd.Parameters.AddWithValue("@ModifiedOn", asset.ModifiedOn);
                cmd.ExecuteNonQuery();
            }

            DeleteExtension(conn, "PropertyAssets", asset.Id);
            DeleteExtension(conn, "VehicleAssets", asset.Id);
            switch (asset)
            {
                case PropertyAsset property:
                    UpsertProperty(conn, property);
                    break;
                case VehicleAsset vehicle:
                    UpsertVehicle(conn, vehicle);
                    break;
            }
        }

        public Asset? Get(SqliteConnection conn, Guid id)
        {
            var rows = Query(conn, BaseSelect + " WHERE Id = @Id;", ("@Id", id.ToString()));
            return rows.Count == 0 ? null : Hydrate(conn, rows[0]);
        }

        public List<Asset> GetAll(SqliteConnection conn)
        {
            var rows = Query(conn, BaseSelect + " ORDER BY Name;");
            return rows.Select(row => Hydrate(conn, row)).OfType<Asset>().ToList();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            DeleteExtension(conn, "PropertyAssets", id);
            DeleteExtension(conn, "VehicleAssets", id);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Assets WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private const string BaseSelect = @"
            SELECT Id, Name, Category, AcquiredOn, CostBasis, HowAcquired, AcquisitionNotes,
                   ClassType, CreatedOn, ModifiedOn
            FROM Assets";

        private sealed record AssetRow(
            Guid Id,
            string Name,
            string Category,
            DateTime? AcquiredOn,
            decimal CostBasis,
            string HowAcquired,
            string AcquisitionNotes,
            string ClassType,
            DateTime CreatedOn,
            DateTime ModifiedOn);

        private static List<AssetRow> Query(SqliteConnection conn, string sql, params (string Name, object Value)[] parameters)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value);
            using var reader = cmd.ExecuteReader();
            var rows = new List<AssetRow>();
            while (reader.Read())
            {
                rows.Add(new AssetRow(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    (decimal)(double)reader.GetDouble(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.GetString(7),
                    reader.GetDateTime(8),
                    reader.GetDateTime(9)));
            }

            return rows;
        }

        private static Asset? Hydrate(SqliteConnection conn, AssetRow row)
        {
            Asset? asset = row.ClassType switch
            {
                nameof(PropertyAsset) => ReadProperty(conn, row.Id),
                nameof(VehicleAsset) => ReadVehicle(conn, row.Id),
                nameof(PersonalAsset) => new PersonalAsset(),
                _ => null
            };
            if (asset is null)
                return null;

            asset.Id = row.Id;
            asset.Name = row.Name;
            asset.Category = row.Category;
            asset.AcquiredOn = row.AcquiredOn;
            asset.CostBasis = row.CostBasis;
            asset.HowAcquired = row.HowAcquired;
            asset.AcquisitionNotes = row.AcquisitionNotes;
            asset.CreatedOn = row.CreatedOn;
            asset.ModifiedOn = row.ModifiedOn;
            return asset;
        }

        private static PropertyAsset ReadProperty(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ParcelAddress, CadNumber, TaxAccountNumber, LegalDescription, LegalAcres, RollCode, AssessmentMethod, LevyMethod
                FROM PropertyAssets
                WHERE AssetId = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return new PropertyAsset();

            var method = reader.FieldCount > 6 && !reader.IsDBNull(6) && Enum.TryParse<AssessmentMethod>(reader.GetString(6), out var parsed)
                ? parsed
                : AssessmentMethod.ApplyHomesteadCap;
            var levy = reader.FieldCount > 7 && !reader.IsDBNull(7) && Enum.TryParse<LevyMethod>(reader.GetString(7), out var parsedLevy)
                ? parsedLevy
                : LevyMethod.RatePerHundred;

            return new PropertyAsset
            {
                ParcelAddress = reader.GetString(0),
                CadNumber = reader.GetString(1),
                TaxAccountNumber = reader.GetString(2),
                LegalDescription = reader.GetString(3),
                LegalAcres = (decimal)(double)reader.GetDouble(4),
                RollCode = reader.GetString(5),
                AssessmentMethod = method,
                LevyMethod = levy
            };
        }

        private static VehicleAsset ReadVehicle(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT VehicleId FROM VehicleAssets WHERE AssetId = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return new VehicleAsset();

            return new VehicleAsset { VehicleId = Guid.Parse(reader.GetString(0)) };
        }

        private static void UpsertProperty(SqliteConnection conn, PropertyAsset property)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PropertyAssets
                    (AssetId, ParcelAddress, CadNumber, TaxAccountNumber, LegalDescription, LegalAcres, RollCode, AssessmentMethod, LevyMethod)
                VALUES
                    (@Id, @Address, @Cad, @TaxAccount, @Description, @Acres, @Roll, @AssessmentMethod, @LevyMethod)
                ON CONFLICT(AssetId) DO UPDATE SET
                    ParcelAddress = excluded.ParcelAddress,
                    CadNumber = excluded.CadNumber,
                    TaxAccountNumber = excluded.TaxAccountNumber,
                    LegalDescription = excluded.LegalDescription,
                    LegalAcres = excluded.LegalAcres,
                    RollCode = excluded.RollCode,
                    AssessmentMethod = excluded.AssessmentMethod,
                    LevyMethod = excluded.LevyMethod;";
            cmd.Parameters.AddWithValue("@Id", property.Id.ToString());
            cmd.Parameters.AddWithValue("@Address", property.ParcelAddress ?? "");
            cmd.Parameters.AddWithValue("@Cad", property.CadNumber ?? "");
            cmd.Parameters.AddWithValue("@TaxAccount", property.TaxAccountNumber ?? "");
            cmd.Parameters.AddWithValue("@Description", property.LegalDescription ?? "");
            cmd.Parameters.AddWithValue("@Acres", property.LegalAcres);
            cmd.Parameters.AddWithValue("@Roll", property.RollCode ?? "");
            cmd.Parameters.AddWithValue("@AssessmentMethod", property.AssessmentMethod.ToString());
            cmd.Parameters.AddWithValue("@LevyMethod", property.LevyMethod.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void UpsertVehicle(SqliteConnection conn, VehicleAsset vehicle)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO VehicleAssets (AssetId, VehicleId)
                VALUES (@Id, @VehicleId)
                ON CONFLICT(AssetId) DO UPDATE SET
                    VehicleId = excluded.VehicleId;";
            cmd.Parameters.AddWithValue("@Id", vehicle.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", vehicle.VehicleId.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void DeleteExtension(SqliteConnection conn, string table, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM {table} WHERE AssetId = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
