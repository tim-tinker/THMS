using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores
{
    public interface IRetirement401kLimitStore
    {
        void Save(Retirement401kLimits limits);
        Retirement401kLimits? Get(int taxYear);
        Retirement401kLimits? GetLatest();
    }
}
