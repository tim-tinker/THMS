using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;

namespace THMS.Logic.Transportation
{
    public class TransportationCostAggregator
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly EvChargeSessionOrchestrator _sessionOrchestrator;

        public TransportationCostAggregator(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore)
            : this(vehicleStore, financeStore, new DataStoreFactory().GetEnergyStore())
        {
        }

        public TransportationCostAggregator(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore)
        {
            _vehicleStore = vehicleStore;
            _sessionOrchestrator = new EvChargeSessionOrchestrator(
                vehicleStore,
                energyStore,
                financeStore);
        }

        // ---------------------------------------------------------
        // PUBLIC API
        // ---------------------------------------------------------

        public TransportationCostSummaryBase GetCostSummary(
            Guid vehicleId,
            DateTime periodStart,
            DateTime periodEnd)
        {
            var vehicle = _vehicleStore.GetVehicle(vehicleId)
                ?? throw new InvalidOperationException("Vehicle not found.");

            return vehicle switch
            {
                VehicleEv ev => GetEvCostSummary(ev, periodStart, periodEnd),
                VehicleIce ice => GetIceCostSummary(ice, periodStart, periodEnd),
                _ => throw new InvalidOperationException("Unknown vehicle type.")
            };
        }

        // ---------------------------------------------------------
        // EV COST SUMMARY
        // ---------------------------------------------------------

        private EvTransportationCostSummary GetEvCostSummary(
            VehicleEv vehicle,
            DateTime start,
            DateTime end)
        {
            var sessions = _sessionOrchestrator
                .GetCompletedSessions(vehicle.Id, start, end);

            var homeCost = sessions.OfType<HomeEvChargeSession>().Sum(s => s.SessionCost);
            var commercialCost = sessions.OfType<CommercialEvChargeSession>().Sum(s => s.SessionCost);
            var totalCost = sessions.Sum(s => s.SessionCost);
            var miles = ComputeEvMiles(sessions);
            var costPerMile = miles > 0 ? totalCost / miles : 0;

            return new EvTransportationCostSummary
            {
                VehicleId = vehicle.Id,
                PeriodStart = start,
                PeriodEnd = end,
                TotalMiles = miles,
                TotalCost = totalCost,
                CostPerMile = costPerMile,
                HomeChargeCost = homeCost,
                CommercialChargeCost = commercialCost
            };
        }

        // ---------------------------------------------------------
        // ICE COST SUMMARY
        // ---------------------------------------------------------

        private IceTransportationCostSummary GetIceCostSummary(
            VehicleIce vehicle,
            DateTime start,
            DateTime end)
        {
            // 1. Get mileage records
            var mileageRecords = _vehicleStore.GetIceMileageRecords(vehicle.Id, start, end)
                .OrderBy(r => r.EndTime)
                .ToList();

            // 2. Compute miles driven
            var miles = ComputeIceMiles(mileageRecords);

            // 3. Total fuel cost, taken from the fill-up captured on each mileage
            // record. The mileage record is the single source of truth for fuel
            // cost; finance GasPurchase rows are not summed here to avoid counting
            // the same fill-up twice.
            var fuelCost = mileageRecords.Sum(r => r.FuelCost);

            // 4. Cost per mile
            var costPerMile = miles > 0 ? fuelCost / miles : 0;

            return new IceTransportationCostSummary
            {
                VehicleId = vehicle.Id,
                PeriodStart = start,
                PeriodEnd = end,
                TotalMiles = miles,
                TotalCost = fuelCost,
                CostPerMile = costPerMile,
                FuelCost = fuelCost
            };
        }

        // ---------------------------------------------------------
        // EV MILES
        // ---------------------------------------------------------

        private static decimal ComputeEvMiles(IReadOnlyList<BaseEvChargeSession> sessions)
        {
            if (sessions.Count == 0)
                return 0;

            var ordered = sessions
                .OrderBy(s => s.StartTime)
                .ThenBy(s => s.EndTime)
                .ToList();

            var miles = ordered[^1].OdometerMiles - ordered[0].LastOdometer;
            return miles > 0 ? miles : 0;
        }

        // ---------------------------------------------------------
        // ICE MILES
        // ---------------------------------------------------------

        private static decimal ComputeIceMiles(IEnumerable<IceMileageRecord> records)
        {
            if (!records.Any())
                return 0;

            var first = records.First().OdometerMiles;
            var last = records.Last().OdometerMiles;

            return last - first;
        }
    }

}
