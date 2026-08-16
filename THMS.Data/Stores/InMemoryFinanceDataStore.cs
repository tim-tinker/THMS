using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public class InMemoryFinanceDataStore : IFinanceDataStore
    {
        private readonly InMemoryElectricUtilityBillStore _utilityBills = new();
        private readonly InMemoryGasPurchaseStore _gasPurchases = new();

        public InMemoryFinanceDataStore()
        {
            UpsertElectricUtilityBill(new ElectricUtilityBill
            {
                Id = Guid.NewGuid(),
                BillingDate = DateTime.Parse("07/18/2026"),
                StartDate = DateTime.Parse("06/17/2026"),
                EndDate = DateTime.Parse("07/17/2026"),
                KwhUsage = 1244,
                DaysInCycle = 30,
                BaseCharge = 9.95m,
                EnergyChargeRate = 0.114997m,
                EnergyCharge = 143.06m,
                ExportKwh = 702,
                ExportCreditRate = 0.064997m,
                ExportCredit = 45.63m,
                DeliveryCharge = 65.94m
            });
        }

        // ---------------------------------------------------------
        // ELECTRIC UTILITY BILLS
        // ---------------------------------------------------------

        public void UpsertElectricUtilityBill(ElectricUtilityBill bill) =>
            _utilityBills.Upsert(bill);

        public ElectricUtilityBill? GetElectricUtilityBill(Guid billId) =>
            _utilityBills.Get(billId);

        public ElectricUtilityBill? GetElectricUtilityBillForDate(DateTime date) =>
            _utilityBills.GetForDate(date);

        public IEnumerable<ElectricUtilityBill> GetElectricUtilityBills(DateTime start, DateTime end) =>
            _utilityBills.GetRange(start, end);

        public ElectricUtilityBill? GetLatestElectricUtilityBill() =>
            _utilityBills.GetLatest();

        // ---------------------------------------------------------
        // GAS PURCHASES
        // ---------------------------------------------------------

        public void UpsertGasPurchase(GasPurchase purchase) =>
            _gasPurchases.Upsert(purchase);

        public IEnumerable<GasPurchase> GetGasPurchases(Guid vehicleId, DateTime start, DateTime end) =>
            _gasPurchases.GetRange(vehicleId, start, end);

    }
}
