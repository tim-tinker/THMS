using THMS.Domain.Assets;

namespace THMS.Data.Stores
{
    public interface IAssetDataStore
    {
        void Save(Asset asset);
        Asset? Get(Guid id);
        IReadOnlyList<Asset> GetAll();
        void Delete(Guid id);

        void SaveValuation(AssetValuation valuation);
        IReadOnlyList<AssetValuation> GetValuations(Guid assetId);
        void DeleteValuation(Guid id);
        void DeleteValuationsForStatement(Guid statementId);
        void ReplaceValuationsForStatement(Guid assetId, Guid statementId, IReadOnlyList<AssetValuation> valuations);
    }
}
