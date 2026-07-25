using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;

namespace THMS.Data.Stores
{
    /// <summary>
    /// Stores all financial-domain objects used by THMS.
    /// Includes utility bills, commercial EV charging costs,
    /// and general financial transactions.
    /// </summary>
    public interface IFinanceDataStore
    {
        // ---------------------------------------------------------
        // ELECTRIC UTILITY BILLS (monthly)
        // ---------------------------------------------------------

        /// <summary>
        /// Adds a monthly electric utility bill.
        /// </summary>
        void AddElectricUtilityBill(ElectricUtilityBill bill);

        /// <summary>
        /// Returns all electric utility bills ordered by start date.
        /// </summary>
        IReadOnlyCollection<ElectricUtilityBill> GetElectricUtilityBills();

        /// <summary>
        /// Raw accessor for logic engines.
        /// </summary>
        IReadOnlyCollection<ElectricUtilityBill> GetAllElectricUtilityBillsRaw();


        // ---------------------------------------------------------
        // COMMERCIAL EV CHARGING COSTS
        // ---------------------------------------------------------

        /// <summary>
        /// Adds a commercial EV charging cost record (e.g., ChargePoint).
        /// Energy (Wh) is stored separately in IEnergyDataStore.
        /// </summary>
        void AddCommercialChargingCostRecord(CommercialChargingCostRecord record);

        /// <summary>
        /// Returns all commercial charging cost records.
        /// </summary>
        IReadOnlyCollection<CommercialChargingCostRecord> GetCommercialChargingCostRecords();

        /// <summary>
        /// Raw accessor for logic engines.
        /// </summary>
        IReadOnlyCollection<CommercialChargingCostRecord> GetAllCommercialChargingCostRecordsRaw();


        // ---------------------------------------------------------
        // GENERAL FINANCE TRANSACTIONS
        // ---------------------------------------------------------

        /// <summary>
        /// Adds a general financial transaction (fuel, maintenance, etc.).
        /// </summary>
        void AddFinanceTransaction(FinanceTransaction transaction);

        /// <summary>
        /// Returns all financial transactions.
        /// </summary>
        IReadOnlyCollection<FinanceTransaction> GetAllTransactions();
    }
}
