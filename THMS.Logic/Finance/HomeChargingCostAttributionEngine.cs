using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Energy;

namespace THMS.Logic.Finance
{
    public class HomeChargingCostAttributionEngine
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly IFinanceDataStore _financeStore;

        public HomeChargingCostAttributionEngine(
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore)
        {
            _energyStore = energyStore;
            _financeStore = financeStore;
        }

        public HomeChargingCostSummary ComputeMonthlyCost(DateTime monthStart, DateTime monthEnd)
        {
            // -----------------------------------------
            // 1. Get the monthly utility bill
            // -----------------------------------------
            var bill = _financeStore
                .GetElectricUtilityBills()
                .FirstOrDefault(b => b.StartDate == monthStart && b.EndDate == monthEnd);

            if (bill == null)
                throw new InvalidOperationException("No utility bill found for the specified month.");

            // -----------------------------------------
            // 2. Get EV circuit readings for the month
            // -----------------------------------------
            var evReadings = _energyStore
                .GetEvCircuitReadings()
                .Where(r => r.Timestamp >= monthStart && r.Timestamp <= monthEnd)
                .OrderBy(r => r.Timestamp)
                .ToList();

            // -----------------------------------------
            // 3. Get solar vendor intervals for the month
            // -----------------------------------------
            var solarIntervals = _energyStore
                .GetSolarVendorIntervals()
                .Where(i => i.Timestamp >= monthStart && i.Timestamp <= monthEnd)
                .OrderBy(i => i.Timestamp)
                .ToList();

            // -----------------------------------------
            // 4. Attribute EV grid energy
            // -----------------------------------------
            decimal evGridWhTotal = 0;

            foreach (var ev in evReadings)
            {
                var interval = solarIntervals
                    .FirstOrDefault(i => i.Timestamp == ev.Timestamp);

                if (interval == null)
                    continue;

                // EV circuit energy for this interval
                decimal evWh = ev.CircuitUseWh;

                // Grid import for this interval
                decimal gridWh = interval.ImportedFromGridWh;

                // EV grid energy = min(EV energy, grid import)
                decimal evGridWh = Math.Min(evWh, gridWh);

                evGridWhTotal += evGridWh;
            }

            decimal evGridKwh = evGridWhTotal / 1000m;

            // -----------------------------------------
            // 5. Compute total bill cost
            // -----------------------------------------
            decimal totalBillCost =
                bill.GridImportCost +
                bill.DeliveryCharges +
                bill.FixedCharges +
                bill.TaxesAndFees -
                bill.GridExportCredit;

            // -----------------------------------------
            // 6. Compute EV share of grid usage
            // -----------------------------------------
            // You will eventually compute total home grid import from SPAN
            decimal totalHomeGridKwh = solarIntervals.Sum(i => i.ImportedFromGridWh) / 1000m;

            decimal evShare = totalHomeGridKwh > 0
                ? evGridKwh / totalHomeGridKwh
                : 0;

            // -----------------------------------------
            // 7. Compute EV cost
            // -----------------------------------------
            decimal evCost = totalBillCost * evShare;

            return new HomeChargingCostSummary
            {
                Start = monthStart,
                End = monthEnd,
                EvGridKwh = evGridKwh,
                EvCost = evCost,
            };
        }
    }

}
