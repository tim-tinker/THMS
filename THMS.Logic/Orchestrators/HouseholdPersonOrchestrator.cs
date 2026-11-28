using THMS.Data.Stores;
using THMS.Domain.Household;

namespace THMS.Logic.Orchestrators
{
    public class HouseholdPersonOrchestrator
    {
        private readonly IHouseholdPersonStore _people;

        public HouseholdPersonOrchestrator()
            : this(new DataStoreFactory().GetHouseholdPersonStore())
        {
        }

        public HouseholdPersonOrchestrator(IHouseholdPersonStore people)
        {
            _people = people;
        }

        public IReadOnlyList<HouseholdPerson> GetAll() => _people.GetAll();

        public HouseholdPerson? Get(Guid id) => _people.Get(id);

        public void Save(HouseholdPerson person)
        {
            ArgumentNullException.ThrowIfNull(person);
            person.Name = person.Name.Trim();
            if (person.Name.Length == 0)
                throw new InvalidOperationException("Name is required.");
            if (person.BirthYear != 0 && (person.BirthYear < 1900 || person.BirthYear > 2100))
                throw new InvalidOperationException("Birth year must be from 1900 through 2100.");
            _people.Save(person);
        }
    }
}
