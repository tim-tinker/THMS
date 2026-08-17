using THMS.Domain.Finance;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryElectricContractStore
    {
        private readonly List<ElectricContract> _items = new();

        public void Upsert(ElectricContract contract)
        {
            var index = _items.FindIndex(c => c.Id == contract.Id);
            if (index < 0)
                _items.Add(contract);
            else
                _items[index] = contract;
        }

        public ElectricContract? Get(Guid contractId) =>
            _items.FirstOrDefault(c => c.Id == contractId);

        public ElectricContract? GetForDate(DateTime date) =>
            _items
                .Where(c => c.StartDate <= date && c.EndDate >= date)
                .OrderByDescending(c => c.EndDate)
                .FirstOrDefault();

        public IEnumerable<ElectricContract> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(c => c.StartDate <= end && c.EndDate >= start)
                .OrderBy(c => c.StartDate);
        }

        public ElectricContract? GetLatest() =>
            _items.OrderByDescending(c => c.StartDate).FirstOrDefault();
    }
}
