using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryMaintenanceInvoiceStore
    {
        private readonly List<MaintenanceInvoiceRecord> _items = new();

        public void Upsert(MaintenanceInvoiceRecord invoice)
        {
            var index = _items.FindIndex(r => r.Id == invoice.Id);
            if (index < 0)
                _items.Add(invoice);
            else
                _items[index] = invoice;
        }

        public IEnumerable<MaintenanceInvoiceRecord> GetRange(Guid vehicleId, DateTime start, DateTime end)
        {
            return _items
                .Where(r => r.VehicleId == vehicleId && r.Date >= start && r.Date <= end)
                .OrderBy(r => r.Date);
        }

        public decimal GetTotalCost(Guid vehicleId, DateTime start, DateTime end)
        {
            return _items
                .Where(r => r.VehicleId == vehicleId && r.Date >= start && r.Date <= end)
                .Sum(r => r.Cost);
        }
    }
}
