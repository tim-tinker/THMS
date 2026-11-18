using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Assets;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteAssetDataStore : IAssetDataStore
    {
        private readonly string _connectionString;
        private readonly AssetsTable _assets = new();
        private readonly AssetValuationsTable _valuations = new();

        public SQLiteAssetDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = Open();
            _assets.InitializeSchema(conn);
            _valuations.InitializeSchema(conn);
        }

        public void Save(Asset asset)
        {
            ArgumentNullException.ThrowIfNull(asset);
            if (asset.Id == Guid.Empty)
                asset.Id = Guid.NewGuid();
            asset.ModifiedOn = DateTime.UtcNow;
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            _assets.Upsert(conn, asset);
            tx.Commit();
            FinanceDataRevision.NoteChanged();
        }

        public Asset? Get(Guid id)
        {
            using var conn = Open();
            return _assets.Get(conn, id);
        }

        public IReadOnlyList<Asset> GetAll()
        {
            using var conn = Open();
            return _assets.GetAll(conn);
        }

        public void Delete(Guid id)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            _valuations.DeleteForAsset(conn, id);
            _assets.Delete(conn, id);
            tx.Commit();
            FinanceDataRevision.NoteChanged();
        }

        public void SaveValuation(AssetValuation valuation)
        {
            ArgumentNullException.ThrowIfNull(valuation);
            if (valuation.Id == Guid.Empty)
                valuation.Id = Guid.NewGuid();
            valuation.ModifiedOn = DateTime.UtcNow;
            using var conn = Open();
            _valuations.Upsert(conn, valuation);
            FinanceDataRevision.NoteChanged();
        }

        public IReadOnlyList<AssetValuation> GetValuations(Guid assetId)
        {
            using var conn = Open();
            return _valuations.GetForAsset(conn, assetId);
        }

        public void DeleteValuation(Guid id)
        {
            using var conn = Open();
            _valuations.Delete(conn, id);
            FinanceDataRevision.NoteChanged();
        }

        public void DeleteValuationsForStatement(Guid statementId)
        {
            using var conn = Open();
            _valuations.DeleteForStatement(conn, statementId);
            FinanceDataRevision.NoteChanged();
        }

        public void ReplaceValuationsForStatement(
            Guid assetId,
            Guid statementId,
            IReadOnlyList<AssetValuation> valuations)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            _valuations.DeleteForStatement(conn, statementId);
            foreach (var valuation in valuations)
            {
                valuation.AssetId = assetId;
                valuation.SourceStatementId = statementId;
                if (valuation.Id == Guid.Empty)
                    valuation.Id = Guid.NewGuid();
                valuation.ModifiedOn = DateTime.UtcNow;
                _valuations.Upsert(conn, valuation);
            }

            tx.Commit();
            FinanceDataRevision.NoteChanged();
        }

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
