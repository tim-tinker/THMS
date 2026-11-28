using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores
{
    public interface IRetirement401kBirthYearStore
    {
        void Save(Retirement401kBirthYear birthYear);
        Retirement401kBirthYear? Get();
    }
}
