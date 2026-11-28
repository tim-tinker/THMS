using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores
{
    public interface IRetirement401kMatchStore
    {
        void Save(Retirement401kMatchFormula formula);
        Retirement401kMatchFormula? Get();
        void SaveFor(Guid personId, string employer, Retirement401kMatchFormula formula);
        Retirement401kMatchFormula? GetFor(Guid personId, string employer);
    }
}
