using THMS.Domain.Household;

namespace THMS.Data.Stores
{
    public interface IHouseholdPersonStore
    {
        void Save(HouseholdPerson person);
        HouseholdPerson? Get(Guid id);
        IReadOnlyList<HouseholdPerson> GetAll();
    }
}
