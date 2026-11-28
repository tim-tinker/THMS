using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores
{
    public class InMemoryRetirement401kMatchStore : IRetirement401kMatchStore
    {
        private readonly List<ScopedMatch> _scoped = [];
        private Retirement401kMatchFormula? _formula;

        public void Save(Retirement401kMatchFormula formula)
        {
            ArgumentNullException.ThrowIfNull(formula);
            _formula = formula;
            FinanceDataRevision.NoteChanged();
        }

        public Retirement401kMatchFormula? Get() => _formula;

        public void SaveFor(Guid personId, string employer, Retirement401kMatchFormula formula)
        {
            ArgumentNullException.ThrowIfNull(formula);
            employer = employer.Trim();
            var index = _scoped.FindIndex(item =>
                item.PersonId == personId
                && string.Equals(item.Employer, employer, StringComparison.OrdinalIgnoreCase));
            var scoped = new ScopedMatch(personId, employer, formula);
            if (index >= 0)
                _scoped[index] = scoped;
            else
                _scoped.Add(scoped);
            FinanceDataRevision.NoteChanged();
        }

        public Retirement401kMatchFormula? GetFor(Guid personId, string employer)
        {
            employer = employer.Trim();
            return _scoped
                .Where(item => item.PersonId == personId
                    && string.Equals(item.Employer, employer, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Formula)
                .FirstOrDefault();
        }

        private sealed record ScopedMatch(Guid PersonId, string Employer, Retirement401kMatchFormula Formula);
    }
}
