using THMS.Domain.Finance;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryCommercialChargeCostStore
    {
        private readonly List<CommercialChargeCostRecord> _items = new();

        public void Upsert(CommercialChargeCostRecord record)
        {
            var index = _items.FindIndex(r => r.Id == record.Id);
            if (index < 0)
                _items.Add(record);
            else
                _items[index] = record;
        }

        public IEnumerable<CommercialChargeCostRecord> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(c => c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date);
        }

        public IEnumerable<CommercialChargeCostRecord> GetRangeByVendor(
            string vendor,
            DateTime start,
            DateTime end)
        {
            return _items
                .Where(c => c.Vendor.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date);
        }
    }
}
