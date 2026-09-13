using THMS.Domain.Finance;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryElectricContractStore
    {
        private readonly List<ElectricContract> _items = new();

        public void Upsert(ElectricContract contract)
        {
            contract.StartDate = contract.StartDate.Date;
            contract.EndDate = contract.EndDate.Date;

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
                .Where(c => c.StartDate.Date <= date.Date && c.EndDate.Date >= date.Date)
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
