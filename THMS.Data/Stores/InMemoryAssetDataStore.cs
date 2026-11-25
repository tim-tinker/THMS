using THMS.Domain.Assets;

namespace THMS.Data.Stores
{
    public class InMemoryAssetDataStore : IAssetDataStore
    {
        private readonly List<Asset> _assets = [];
        private readonly List<AssetValuation> _valuations = [];
        private readonly List<ValuationSource> _sources = [];

        public void Save(Asset asset)
        {
            ArgumentNullException.ThrowIfNull(asset);
            if (asset.Id == Guid.Empty)
                asset.Id = Guid.NewGuid();
            asset.ModifiedOn = DateTime.UtcNow;

            var index = _assets.FindIndex(item => item.Id == asset.Id);
            if (index >= 0)
                _assets[index] = asset;
            else
                _assets.Add(asset);
            FinanceDataRevision.NoteChanged();
        }

        public Asset? Get(Guid id) => _assets.FirstOrDefault(item => item.Id == id);

        public IReadOnlyList<Asset> GetAll() =>
            _assets.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

        public void Delete(Guid id)
        {
            _assets.RemoveAll(item => item.Id == id);
            _valuations.RemoveAll(item => item.AssetId == id);
            FinanceDataRevision.NoteChanged();
        }

        public void SaveSource(ValuationSource source)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (source.Id == Guid.Empty)
                source.Id = Guid.NewGuid();
            source.ModifiedOn = DateTime.UtcNow;
            var index = _sources.FindIndex(item => item.Id == source.Id);
            if (index >= 0)
                _sources[index] = source;
            else
                _sources.Add(source);
            FinanceDataRevision.NoteChanged();
        }

        public IReadOnlyList<ValuationSource> GetSources() =>
            _sources.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

        public void DeleteSource(Guid id)
        {
            _sources.RemoveAll(item => item.Id == id);
            FinanceDataRevision.NoteChanged();
        }

        public void SaveValuation(AssetValuation valuation)
        {
            ArgumentNullException.ThrowIfNull(valuation);
            if (valuation.Id == Guid.Empty)
                valuation.Id = Guid.NewGuid();
            valuation.ModifiedOn = DateTime.UtcNow;

            var index = _valuations.FindIndex(item => item.Id == valuation.Id);
            if (index >= 0)
                _valuations[index] = valuation;
            else
                _valuations.Add(valuation);
            FinanceDataRevision.NoteChanged();
        }

        public IReadOnlyList<AssetValuation> GetValuations(Guid assetId) =>
            _valuations
                .Where(item => item.AssetId == assetId)
                .OrderByDescending(item => item.AsOf)
                .ThenBy(item => item.ValueType, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public void DeleteValuation(Guid id)
        {
            _valuations.RemoveAll(item => item.Id == id);
            FinanceDataRevision.NoteChanged();
        }

        public void DeleteValuationsForStatement(Guid statementId)
        {
            _valuations.RemoveAll(item => item.SourceStatementId == statementId);
            FinanceDataRevision.NoteChanged();
        }

        public void ReplaceValuationsForStatement(
            Guid assetId,
            Guid statementId,
            IReadOnlyList<AssetValuation> valuations)
        {
            _valuations.RemoveAll(item => item.SourceStatementId == statementId);
            foreach (var valuation in valuations)
            {
                valuation.AssetId = assetId;
                valuation.SourceStatementId = statementId;
                if (valuation.Id == Guid.Empty)
                    valuation.Id = Guid.NewGuid();
                valuation.ModifiedOn = DateTime.UtcNow;
                _valuations.Add(valuation);
            }

            FinanceDataRevision.NoteChanged();
        }
    }
}
