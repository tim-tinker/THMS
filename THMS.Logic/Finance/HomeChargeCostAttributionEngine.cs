using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Energy;

namespace THMS.Logic.Finance
{
    public class HomeChargeCostAttributionEngine
    {
        private IFinanceDataStore? _financeStore;
        private IEnergyDataStore? _energyStore;

        public HomeChargeCostAttributionEngine() { }

        public HomeChargeCostAttributionEngine(
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore)
        {
            _energyStore = energyStore;
            _financeStore = financeStore;
        }

        public void SetStores(
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore)
        {
            _financeStore = financeStore;
            _energyStore = energyStore;
        }

        public HomeChargeCostSummary ComputeMonthlyCost(DateTime monthStart, DateTime monthEnd)
        {
            // -----------------------------------------
            // 1. Get the monthly utility bill
            // -----------------------------------------
            var bill = _financeStore
                .GetElectricUtilityBills(monthStart, monthEnd)
                .FirstOrDefault();

            if (bill == null)
                throw new InvalidOperationException("No utility bill found for the specified month.");

            // -----------------------------------------
            // 2. Get EV circuit readings for the month
            // -----------------------------------------
            var evReadings = _energyStore
                .GetEvCircuitReadings(monthStart, monthEnd)
                .OrderBy(r => r.Timestamp)
                .ToList();

            // -----------------------------------------
            // 3. Get solar vendor intervals for the month
            // -----------------------------------------
            var solarIntervals = _energyStore
                .GetSolarVendorIntervals(monthStart, monthEnd)
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
                decimal evWh = ev.KiloWattHours;

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

            return new HomeChargeCostSummary
            {
                Start = monthStart,
                End = monthEnd,
                EvGridKwh = evGridKwh,
                EvCost = evCost,
            };
        }
    }

}
