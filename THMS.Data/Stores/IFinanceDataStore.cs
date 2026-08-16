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
    }
}
