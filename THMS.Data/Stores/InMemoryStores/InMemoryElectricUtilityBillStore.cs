using THMS.Domain.Finance.Billing;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryElectricUtilityBillStore
    {
        private readonly List<ElectricUtilityBill> _items = new();

        public void Upsert(ElectricUtilityBill bill)
        {
            var existing = _items.FirstOrDefault(b => b.EndDate == bill.EndDate);
            if (existing is null)
            {
                _items.Add(bill);
                return;
            }

            existing.StartDate = bill.StartDate;
            existing.GridImportCost = bill.GridImportCost;
            existing.GridExportCredit = bill.GridExportCredit;
            existing.DeliveryCharges = bill.DeliveryCharges;
            existing.FixedCharges = bill.FixedCharges;
            existing.TaxesAndFees = bill.TaxesAndFees;
            existing.TotalKwh = bill.TotalKwh;
            if (existing.Id == Guid.Empty && bill.Id != Guid.Empty)
                existing.Id = bill.Id;
        }

        public IEnumerable<ElectricUtilityBill> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(b => b.StartDate >= start && b.EndDate <= end)
                .OrderBy(b => b.StartDate);
        }
    }
}
