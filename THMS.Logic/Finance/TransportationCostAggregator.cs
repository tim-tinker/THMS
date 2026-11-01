using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;

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

        public IReadOnlyList<EvChargeSessionContribution> GetChargeSessions(
            Guid vehicleId,
            DateTime periodStart,
            DateTime periodEnd)
        {
            if (_vehicleStore.GetVehicle(vehicleId) is not VehicleEv vehicle)
                return [];

            return ToChargeRows(LoadSessions(vehicle, periodStart, periodEnd));
        }

        public IReadOnlyList<IceFillUpContribution> GetFillUps(
            Guid vehicleId,
            DateTime periodStart,
            DateTime periodEnd)
        {
            if (_vehicleStore.GetVehicle(vehicleId) is not VehicleIce vehicle)
                return [];

            return ToFillUpRows(LoadFillUps(vehicle, periodStart, periodEnd));
        }

        public void ApplyTo(
            VehicleListItemViewModel item,
            VehicleBase vehicle,
            DateTime periodStart,
            DateTime periodEnd)
        {
            switch (vehicle)
            {
                case VehicleEv ev:
                    var sessions = LoadSessions(ev, periodStart, periodEnd);
                    item.ApplySummary(SummarizeEv(ev, periodStart, periodEnd, sessions));
                    item.SetContributions(ToChargeRows(sessions), [], MonthsFromSessions(ev, sessions));
                    break;
                case VehicleIce ice:
                    var fillUps = LoadFillUps(ice, periodStart, periodEnd);
                    item.ApplySummary(SummarizeIce(ice, periodStart, periodEnd, fillUps));
                    item.SetContributions([], ToFillUpRows(fillUps), MonthsFromFillUps(ice, fillUps));
                    break;
                default:
                    throw new InvalidOperationException("Unknown vehicle type.");
            }
        }

        // ---------------------------------------------------------
        // EV COST SUMMARY
        // ---------------------------------------------------------

        private EvTransportationCostSummary GetEvCostSummary(
            VehicleEv vehicle,
            DateTime start,
            DateTime end) =>
            SummarizeEv(vehicle, start, end, LoadSessions(vehicle, start, end));

        private IReadOnlyList<BaseEvChargeSession> LoadSessions(
            VehicleEv vehicle,
            DateTime start,
            DateTime end) =>
            _sessionOrchestrator.GetCompletedSessions(vehicle.Id, start, end);

        private static EvTransportationCostSummary SummarizeEv(
            VehicleEv vehicle,
            DateTime start,
            DateTime end,
            IReadOnlyList<BaseEvChargeSession> sessions)
        {
            var homeCost = sessions.OfType<HomeEvChargeSession>().Sum(s => s.SessionCost);
            var commercialCost = sessions.OfType<CommercialEvChargeSession>().Sum(s => s.SessionCost);
            var totalCost = sessions.Sum(s => s.SessionCost);
            var miles = ComputeEvMiles(sessions);
            var gallons = sessions.Sum(session => session.KwhDrawn ?? 0m) * 1000m / WattHoursPerGallon;

            return new EvTransportationCostSummary
            {
                VehicleId = vehicle.Id,
                PeriodStart = start,
                PeriodEnd = end,
                TotalMiles = miles,
                TotalCost = totalCost,
                CostPerMile = CostPerMile(totalCost, miles),
                Gallons = gallons,
                Mpg = MilesPerGallon(miles, gallons),
                HomeChargeCost = homeCost,
                CommercialChargeCost = commercialCost
            };
        }

        private static List<EvChargeSessionContribution> ToChargeRows(
            IReadOnlyList<BaseEvChargeSession> sessions) =>
            sessions
                .OrderByDescending(session => session.StartTime)
                .ThenByDescending(session => session.EndTime)
                .Select(session =>
                {
                    var drawn = session.KwhDrawn ?? 0m;
                    var gallons = drawn * 1000m / WattHoursPerGallon;
                    var miles = session.OdometerMiles - session.LastOdometer;
                    if (miles < 0)
                        miles = 0;

                    return new EvChargeSessionContribution
                    {
                        EndTime = session.EndTime,
                        Mpg = MilesPerGallon(miles, gallons),
                        CostPerMile = CostPerMile(session.SessionCost, miles),
                        IsHome = session.IsHomeCharge,
                        Cost = session.SessionCost,
                        Miles = miles,
                        Gallons = gallons
                    };
                })
                .ToList();

        private static List<VehicleMonthContribution> MonthsFromSessions(
            VehicleEv vehicle,
            IReadOnlyList<BaseEvChargeSession> sessions) =>
            sessions
                .GroupBy(session => new DateTime(session.EndTime.Year, session.EndTime.Month, 1))
                .OrderByDescending(group => group.Key)
                .Select(group =>
                {
                    var monthSessions = group.ToList();
                    var summary = SummarizeEv(vehicle, group.Key, group.Key.AddMonths(1).AddTicks(-1), monthSessions);
                    return new VehicleMonthContribution
                    {
                        Month = group.Key,
                        Mpg = summary.Mpg,
                        CostPerMile = summary.CostPerMile,
                        TotalCost = summary.TotalCost,
                        TotalMiles = summary.TotalMiles,
                        Gallons = summary.Gallons,
                        ChargeSessions = ToChargeRows(monthSessions)
                    };
                })
                .ToList();

        private static List<VehicleMonthContribution> MonthsFromFillUps(
            VehicleIce vehicle,
            IReadOnlyList<IceMileageRecord> fillUps) =>
            fillUps
                .GroupBy(record => new DateTime(record.EndTime.Year, record.EndTime.Month, 1))
                .OrderByDescending(group => group.Key)
                .Select(group =>
                {
                    var monthFillUps = group.OrderBy(record => record.EndTime).ToList();
                    var summary = SummarizeIce(vehicle, group.Key, group.Key.AddMonths(1).AddTicks(-1), monthFillUps);
                    return new VehicleMonthContribution
                    {
                        Month = group.Key,
                        Mpg = summary.Mpg,
                        CostPerMile = summary.CostPerMile,
                        TotalCost = summary.TotalCost,
                        TotalMiles = summary.TotalMiles,
                        Gallons = summary.Gallons,
                        FillUps = ToFillUpRows(monthFillUps)
                    };
                })
                .ToList();

        // ---------------------------------------------------------
        // ICE COST SUMMARY
        // ---------------------------------------------------------

        private IceTransportationCostSummary GetIceCostSummary(
            VehicleIce vehicle,
            DateTime start,
            DateTime end) =>
            SummarizeIce(vehicle, start, end, LoadFillUps(vehicle, start, end));

        private List<IceMileageRecord> LoadFillUps(
            VehicleIce vehicle,
            DateTime start,
            DateTime end) =>
            _vehicleStore.GetIceMileageRecords(vehicle.Id, start, end)
                .OrderBy(record => record.EndTime)
                .ToList();

        private static IceTransportationCostSummary SummarizeIce(
            VehicleIce vehicle,
            DateTime start,
            DateTime end,
            IReadOnlyList<IceMileageRecord> mileageRecords)
        {
            // Fuel cost is taken from the fill-up on each mileage record. Those
            // records are the source of truth; finance GasPurchase rows are not
            // summed here, so the same fill-up is not counted twice.
            var fuelCost = mileageRecords.Sum(record => record.FuelCost);
            var gallons = mileageRecords.Sum(record => record.GallonsAdded);
            var miles = ComputeIceMiles(mileageRecords);

            return new IceTransportationCostSummary
            {
                VehicleId = vehicle.Id,
                PeriodStart = start,
                PeriodEnd = end,
                TotalMiles = miles,
                TotalCost = fuelCost,
                CostPerMile = CostPerMile(fuelCost, miles),
                Gallons = gallons,
                Mpg = MilesPerGallon(miles, gallons),
                FuelCost = fuelCost
            };
        }

        private static List<IceFillUpContribution> ToFillUpRows(
            IReadOnlyList<IceMileageRecord> mileageRecords) =>
            mileageRecords
                .OrderByDescending(record => record.EndTime)
                .Select(record => new IceFillUpContribution
                {
                    Date = record.EndTime,
                    OdometerMiles = record.OdometerMiles,
                    Gallons = record.GallonsAdded,
                    Cost = record.FuelCost
                })
                .ToList();

        // EPA gasoline gallon equivalent.
        private const decimal WattHoursPerGallon = 33700m;

        private static decimal CostPerMile(decimal totalCost, decimal miles) =>
            miles > 0 ? decimal.Round(totalCost / miles, 4, MidpointRounding.AwayFromZero) : 0;

        private static decimal MilesPerGallon(decimal miles, decimal gallons) =>
            gallons > 0 ? miles / gallons : 0;

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
