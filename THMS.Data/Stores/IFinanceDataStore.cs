using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public interface IFinanceDataStore
    {
        // ---------------------------------------------------------
        // ELECTRIC UTILITY BILLS (HOME ENERGY)
        // ---------------------------------------------------------

        void AddElectricUtilityBill(ElectricUtilityBill bill);

        IEnumerable<ElectricUtilityBill> GetElectricUtilityBills(
            DateTime start,
            DateTime end);


        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS (RAW ENERGY DOMAIN)
        // ---------------------------------------------------------

        void AddCommercialChargeCostRecord(CommercialChargeCostRecord record);

        IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecords(
            DateTime start,
            DateTime end);

        IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecordsByVendor(
            string vendor,
            DateTime start,
            DateTime end);

        // ---------------------------------------------------------
        // GAS PURCHASES (ICE VEHICLES)
        // ---------------------------------------------------------

        void AddGasPurchase(GasPurchase purchase);

        IEnumerable<GasPurchase> GetGasPurchases(
            Guid vehicleId,
            DateTime start,
            DateTime end);


        // ---------------------------------------------------------
        // INCOMPLETE COST RECORDS (USER CORRECTION WORKFLOW)
        // ---------------------------------------------------------

        IEnumerable<EvChargeSession> GetEvChargeSessionsWithMissingCost();

        IEnumerable<GasPurchase> GetGasPurchasesWithMissingCost();


        // ---------------------------------------------------------
        // COST UPDATES (ONLY WHERE NECESSARY)
        // ---------------------------------------------------------

        void UpdateEvChargeSessionCost(Guid sessionId, decimal cost);

        void UpdateGasPurchaseCost(Guid purchaseId, decimal cost);
    }
}
