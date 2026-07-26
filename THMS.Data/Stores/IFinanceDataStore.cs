using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public interface IFinanceStore
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

        void AddCommercialChargingCostRecord(CommercialChargingCostRecord record);

        IEnumerable<CommercialChargingCostRecord> GetCommercialChargingCostRecords(
            DateTime start,
            DateTime end);

        IEnumerable<CommercialChargingCostRecord> GetCommercialChargingCostRecordsByVendor(
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

        IEnumerable<EvChargingSession> GetEvChargingSessionsWithMissingCost();

        IEnumerable<GasPurchase> GetGasPurchasesWithMissingCost();


        // ---------------------------------------------------------
        // COST UPDATES (ONLY WHERE NECESSARY)
        // ---------------------------------------------------------

        void UpdateEvChargingSessionCost(Guid sessionId, decimal cost);

        void UpdateGasPurchaseCost(Guid purchaseId, decimal cost);
    }
}
