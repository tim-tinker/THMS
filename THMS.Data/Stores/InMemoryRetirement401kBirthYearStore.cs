using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores
{
    public class InMemoryRetirement401kBirthYearStore : IRetirement401kBirthYearStore
    {
        private Retirement401kBirthYear? _birthYear;

        public void Save(Retirement401kBirthYear birthYear)
        {
            ArgumentNullException.ThrowIfNull(birthYear);
            _birthYear = birthYear;
            FinanceDataRevision.NoteChanged();
        }

        public Retirement401kBirthYear? Get() => _birthYear;
    }
}
