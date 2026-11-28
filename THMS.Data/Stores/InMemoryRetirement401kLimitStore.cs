using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores
{
    public class InMemoryRetirement401kLimitStore : IRetirement401kLimitStore
    {
        private readonly List<Retirement401kLimits> _limits = [];

        public void Save(Retirement401kLimits limits)
        {
            ArgumentNullException.ThrowIfNull(limits);
            var index = _limits.FindIndex(item => item.TaxYear == limits.TaxYear);
            if (index >= 0)
                _limits[index] = limits;
            else
                _limits.Add(limits);
            FinanceDataRevision.NoteChanged();
        }

        public Retirement401kLimits? Get(int taxYear) =>
            _limits.FirstOrDefault(item => item.TaxYear == taxYear);

        public Retirement401kLimits? GetLatest() =>
            _limits.OrderByDescending(item => item.TaxYear).FirstOrDefault();
    }
}
