using System;
using System.Collections.Generic;
using System.Text;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Logic.Finance;

namespace THMS.Logic.ViewModels.Finance
{
    /// <summary>
    /// Aggregates all transportation-related costs for a given month.
    /// Combines EV charging (home + commercial) with gasoline, maintenance,
    /// insurance, registration, parking, and tolls.
    /// </summary>
    public class TransportationCostAggregator
    {
        private readonly HomeChargingCostAttributionEngine _homeEngine;
        private readonly CommercialChargingCostEngine _commercialEngine;
        private readonly IFinanceDataStore _financeStore;

        public TransportationCostAggregator(
            HomeChargingCostAttributionEngine homeEngine,
            CommercialChargingCostEngine commercialEngine,
            IFinanceDataStore financeStore)
        {
            _homeEngine = homeEngine;
            _commercialEngine = commercialEngine;
            _financeStore = financeStore;
        }

        /// <summary>
        /// Computes the complete transportation cost summary for the month.
        /// </summary>
        public TransportationCostSummary ComputeMonthlySummary(DateTime monthStart, DateTime monthEnd)
        {
            var home = _homeEngine.ComputeMonthlyCost(monthStart, monthEnd);
            var commercial = _commercialEngine.ComputeSummary(monthStart, monthEnd);

            var transactions = _financeStore.GetAllTransactions()
                .Where(t => t.Date >= monthStart && t.Date <= monthEnd)
                .ToList();

            return new TransportationCostSummary
            {
                Start = monthStart,
                End = monthEnd,

                HomeEvChargingCost = home.EvCost,
                CommercialEvChargingCost = commercial.TotalCost,

                GasCost = Sum(transactions, "Gas"),
                MaintenanceCost = Sum(transactions, "Maintenance"),
                InsuranceCost = Sum(transactions, "Insurance"),
                RegistrationCost = Sum(transactions, "Registration"),
                ParkingCost = Sum(transactions, "Parking"),
                TollCost = Sum(transactions, "Toll")
            };
        }

        private static decimal Sum(IEnumerable<FinanceTransaction> tx, string source) =>
            tx.Where(t => t.Source == source).Sum(t => t.Amount);
    }
}
