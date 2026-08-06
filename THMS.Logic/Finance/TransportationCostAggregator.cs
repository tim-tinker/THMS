using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Transportation;
using static System.Collections.Specialized.BitVector32;

namespace THMS.Logic.Transportation
{
    public class TransportationCostAggregator
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IFinanceDataStore _financeStore;

        public TransportationCostAggregator(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore)
        {
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
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
            // 1. Get enriched EV charging sessions
            var sessions = _vehicleStore.GetEvChargingSessions(vehicle.Id, start, end)
                .ToList();

            // 2. Split home vs commercial
            var homeSessions = sessions.Where(s => s.IsHomeCharging).ToList();
            var commercialSessions = sessions.Where(s => !s.IsHomeCharging).ToList();

            // 3. Home charging cost attribution
            var homeCost = ComputeHomeChargingCost(homeSessions, start, end);

            // 4. Commercial charging cost (direct)
            var commercialCost = commercialSessions
                .Sum(s => s.SessionCost);

            // 5. Total EV miles
            var miles = ComputeEvMiles(sessions);

            // 6. Cost per mile
            var totalCost = homeCost + commercialCost;
            var costPerMile = miles > 0 ? totalCost / miles : 0;

            return new EvTransportationCostSummary
            {
                VehicleId = vehicle.Id,
                PeriodStart = start,
                PeriodEnd = end,
                TotalMiles = miles,
                TotalCost = totalCost,
                CostPerMile = costPerMile,
                HomeChargingCost = homeCost,
                CommercialChargingCost = commercialCost
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
                .OrderBy(r => r.Date)
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
        // HOME CHARGING COST ATTRIBUTION
        // ---------------------------------------------------------

        private decimal ComputeHomeChargingCost(
            IEnumerable<EvChargingSession> homeSessions,
            DateTime start,
            DateTime end)
        {
            // 1. Get utility bills for the period
            var bills = _financeStore.GetElectricUtilityBills(start, end).ToList();
            if (!bills.Any())
                return 0;

            // 2. Compute cost per kWh for each bill
            var costPerKwh = bills.Select(b =>
                b.TotalCost / (b.TotalKwh == 0 ? 1 : b.TotalKwh)).ToList();

            // 3. Compute average cost per kWh
            var avgCostPerKwh = costPerKwh.Average();

            // 4. Compute total kWh added
            var totalKwh = homeSessions.Sum(s => s.KwhAdded);

            // 5. Cost = kWh * avg cost per kWh
            return totalKwh * avgCostPerKwh;
        }

        // ---------------------------------------------------------
        // EV MILES
        // ---------------------------------------------------------

        private decimal ComputeEvMiles(IEnumerable<EvChargingSession> sessions)
        {
            decimal miles = 0;

            var validSessions = (from session in sessions
                                 orderby session.EndTime
                                 select session).ToArray();

            var startSession = validSessions.FirstOrDefault();
            var endSession = validSessions.LastOrDefault();
            if (startSession != null && endSession != null)
            {
                miles = GetOdometer(endSession) - GetOdometer(startSession);
            }

            return miles;
        }

        private decimal GetOdometer(EvChargingSession session)
        {
            decimal odometer = session.OdometerMiles;
            return odometer;
        }

        // ---------------------------------------------------------
        // ICE MILES
        // ---------------------------------------------------------

        private decimal ComputeIceMiles(IEnumerable<IceMileageRecord> records)
        {
            if (!records.Any())
                return 0;

            var first = records.First().OdometerMiles;
            var last = records.Last().OdometerMiles;

            return last - first;
        }
    }

}
