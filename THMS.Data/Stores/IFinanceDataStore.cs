using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    public interface IFinanceDataStore
    {
        // ---------------------------------------------------------
        // ELECTRIC UTILITY BILLS
        // ---------------------------------------------------------

        /// <summary>Upsert key: <see cref="ElectricUtilityBill.Id"/>.</summary>
        void UpsertElectricUtilityBill(ElectricUtilityBill bill);

        /// <summary>Get a bill by its unique identifier.</summary>
        ElectricUtilityBill? GetElectricUtilityBill(Guid billId);

        /// <summary>
        /// Get the bill whose billing cycle covers the specified date.
        /// Example: session.StartTime falls between bill.StartDate and bill.EndDate.
        /// </summary>
        ElectricUtilityBill? GetElectricUtilityBillForDate(DateTime date);

        /// <summary>
        /// Get all bills whose StartDate or EndDate falls within the given range.
        /// </summary>
        IEnumerable<ElectricUtilityBill> GetElectricUtilityBills(DateTime start, DateTime end);

        /// <summary>Most recent bill by <see cref="ElectricUtilityBill.EndDate"/>, or null if none.</summary>
        ElectricUtilityBill? GetLatestElectricUtilityBill();

        // ---------------------------------------------------------
        // GAS PURCHASES (ICE VEHICLES)
        // ---------------------------------------------------------

        /// <summary>Upsert key: <see cref="GasPurchase.Id"/>.</summary>
        void UpsertGasPurchase(GasPurchase purchase);

        IEnumerable<GasPurchase> GetGasPurchases(
            Guid vehicleId,
            DateTime start,
            DateTime end);

        // ---------------------------------------------------------
        // ELECTRIC CONTRACTS
        // ---------------------------------------------------------

        /// <summary>
        /// Upsert key: <see cref="ElectricContract.Id"/>.
        /// </summary>
        void UpsertElectricContract(ElectricContract contract);

        /// <summary>
        /// Get a contract by its unique identifier.
        /// </summary>
        ElectricContract? GetElectricContract(Guid contractId);

        /// <summary>
        /// Get the contract whose date range covers the specified date.
        /// Example: session.StartTime falls between contract.StartDate and contract.EndDate.
        /// </summary>
        ElectricContract? GetElectricContractForDate(DateTime date);

        /// <summary>
        /// Get all contracts whose date ranges overlap the given range.
        /// </summary>
        IEnumerable<ElectricContract> GetElectricContracts(DateTime start, DateTime end);

        /// <summary>
        /// Most recent contract by <see cref="ElectricContract.StartDate"/>, or null if none.
        /// </summary>
        ElectricContract? GetLatestElectricContract();

    }
}
