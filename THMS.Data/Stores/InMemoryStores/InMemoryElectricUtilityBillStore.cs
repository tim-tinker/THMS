using THMS.Domain.Finance.Billing;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryElectricUtilityBillStore
    {
        private readonly List<ElectricUtilityBill> _items = new();

        public void Upsert(ElectricUtilityBill bill)
        {
            var existing = _items.FirstOrDefault(b => b.Id == bill.Id);
            if (existing is null)
            {
                _items.Add(bill);
                return;
            }

            existing.BillingDate = bill.BillingDate;
            existing.StartDate = bill.StartDate;
            existing.EndDate = bill.EndDate;
            existing.KwhUsage = bill.KwhUsage;
            existing.DaysInCycle = bill.DaysInCycle;
            existing.BaseCharge = bill.BaseCharge;
            existing.EnergyChargeRate = bill.EnergyChargeRate;
            existing.EnergyCharge = bill.EnergyCharge;
            existing.ExportKwh = bill.ExportKwh;
            existing.ExportCreditRate = bill.ExportCreditRate;
            existing.ExportCredit = bill.ExportCredit;
            existing.DeliveryCharge = bill.DeliveryCharge;
        }

        public ElectricUtilityBill? Get(Guid billId) =>
            _items.FirstOrDefault(b => b.Id == billId);

        public ElectricUtilityBill? GetForDate(DateTime date) =>
            _items
                .Where(b => b.StartDate <= date && b.EndDate >= date)
                .OrderByDescending(b => b.EndDate)
                .FirstOrDefault();

        public IEnumerable<ElectricUtilityBill> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(b =>
                    (b.StartDate >= start && b.StartDate <= end) ||
                    (b.EndDate >= start && b.EndDate <= end))
                .OrderBy(b => b.StartDate);
        }

        public ElectricUtilityBill? GetLatest() =>
            _items.OrderByDescending(b => b.EndDate).FirstOrDefault();
    }
}
