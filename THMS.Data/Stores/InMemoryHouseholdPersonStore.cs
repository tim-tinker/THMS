using THMS.Domain.Household;

namespace THMS.Data.Stores
{
    public class InMemoryHouseholdPersonStore : IHouseholdPersonStore
    {
        private readonly List<HouseholdPerson> _people = [];

        public void Save(HouseholdPerson person)
        {
            ArgumentNullException.ThrowIfNull(person);
            if (person.Id == Guid.Empty)
                person.Id = Guid.NewGuid();
            person.ModifiedOn = DateTime.UtcNow;
            var copy = Copy(person);
            var index = _people.FindIndex(item => item.Id == copy.Id);
            if (index >= 0)
                _people[index] = copy;
            else
                _people.Add(copy);
            FinanceDataRevision.NoteChanged();
        }

        public HouseholdPerson? Get(Guid id) =>
            _people.Where(person => person.Id == id).Select(Copy).FirstOrDefault();

        public IReadOnlyList<HouseholdPerson> GetAll() =>
            _people
                .OrderBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
                .Select(Copy)
                .ToList();

        private static HouseholdPerson Copy(HouseholdPerson person) => new()
        {
            Id = person.Id,
            CreatedOn = person.CreatedOn,
            ModifiedOn = person.ModifiedOn,
            Name = person.Name,
            BirthYear = person.BirthYear
        };
    }
}
