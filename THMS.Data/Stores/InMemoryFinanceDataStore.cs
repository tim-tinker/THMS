using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public class InMemoryFinanceDataStore : IFinanceDataStore
    {
        private readonly InMemoryElectricUtilityBillStore _utilityBills = new();
        private readonly InMemoryCommercialChargeCostStore _commercialCosts = new();
        private readonly InMemoryGasPurchaseStore _gasPurchases = new();
        private readonly InMemoryEvChargeSessionCostStore _evChargeSessionCosts = new();

        public InMemoryFinanceDataStore()
        {
            UpsertElectricUtilityBill(new ElectricUtilityBill
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.Today.AddMonths(-1).AddDays(-5),
                EndDate = DateTime.Today.AddMonths(-1).AddDays(25),
                GridImportCost = 85.00m,
                GridExportCredit = 12.50m,
                DeliveryCharges = 22.00m,
                FixedCharges = 10.00m,
                TaxesAndFees = 8.75m,
                TotalKwh = 650
            });

            UpsertGasPurchase(new GasPurchase
            {
                Id = Guid.NewGuid(),
                VehicleId = Guid.Empty,
                Date = DateTime.Today.AddDays(-7),
                Gallons = 11.2m,
                FuelCost = 34.80m,
                Station = "Shell"
            });

            UpsertGasPurchase(new GasPurchase
            {
                Id = Guid.NewGuid(),
                VehicleId = Guid.Empty,
                Date = DateTime.Today.AddDays(-2),
                Gallons = 12.0m,
                FuelCost = 38.10m,
                Station = "Chevron"
            });

            UpsertCommercialChargeCostRecord(new CommercialChargeCostRecord
            {
                Id = Guid.NewGuid(),
                SessionId = "EA-2026-07-22-ABC123",
                Date = DateTime.Today.AddDays(-2),
                Cost = 11.99m,
                Vendor = "Electrify America"
            });

            UpsertCommercialChargeCostRecord(new CommercialChargeCostRecord
            {
                Id = Guid.NewGuid(),
                SessionId = "CP-2026-07-20-XYZ789",
                Date = DateTime.Today.AddDays(-5),
                Cost = 9.50m,
                Vendor = "ChargePoint"
            });
        }

        // ---------------------------------------------------------
        // ELECTRIC UTILITY BILLS
        // ---------------------------------------------------------

        public void UpsertElectricUtilityBill(ElectricUtilityBill bill) =>
            _utilityBills.Upsert(bill);

        public IEnumerable<ElectricUtilityBill> GetElectricUtilityBills(DateTime start, DateTime end) =>
            _utilityBills.GetRange(start, end);

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS
        // ---------------------------------------------------------

        public void UpsertCommercialChargeCostRecord(CommercialChargeCostRecord record) =>
            _commercialCosts.Upsert(record);

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecords(DateTime start, DateTime end) =>
            _commercialCosts.GetRange(start, end);

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecordsByVendor(
            string vendor,
            DateTime start,
            DateTime end) =>
            _commercialCosts.GetRangeByVendor(vendor, start, end);

        // ---------------------------------------------------------
        // GAS PURCHASES
        // ---------------------------------------------------------

        public void UpsertGasPurchase(GasPurchase purchase) =>
            _gasPurchases.Upsert(purchase);

        public IEnumerable<GasPurchase> GetGasPurchases(Guid vehicleId, DateTime start, DateTime end) =>
            _gasPurchases.GetRange(vehicleId, start, end);

        // ---------------------------------------------------------
        // INCOMPLETE COST RECORDS
        // ---------------------------------------------------------

        public IEnumerable<EvChargeSession> GetEvChargeSessionsWithMissingCost() =>
            _evChargeSessionCosts.GetWithMissingCost();

        public IEnumerable<GasPurchase> GetGasPurchasesWithMissingCost() =>
            _gasPurchases.GetWithMissingCost();

        // ---------------------------------------------------------
        // COST UPDATES
        // ---------------------------------------------------------

        public void UpdateEvChargeSessionCost(Guid sessionId, decimal cost) =>
            _evChargeSessionCosts.UpdateCost(sessionId, cost);

        public void UpdateGasPurchaseCost(Guid purchaseId, decimal cost) =>
            _gasPurchases.UpdateCost(purchaseId, cost);
    }
}
