using THMS.Data.Stores;
using THMS.Domain.Transportation.Analytics;

namespace THMS.Logic.Transportation
{
    /// <summary>
    /// Computes transportation analytics such as cost per mile,
    /// charging cost ratios, and monthly summaries.
    /// </summary>
    public class TransportationAnalyticsEngine
    {
        private readonly TransportationDataStore _store;

        public TransportationAnalyticsEngine(TransportationDataStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Computes a monthly summary for a specific vehicle.
        /// </summary>
        public MonthlyTransportationSummary ComputeMonthlySummary(Guid vehicleId, int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddSeconds(-1);

            // Mileage
            var milesDriven = _store.GetMilesDrivenInPeriod(vehicleId, start, end);

            // Charging cost (commercial)
            var chargingCost = _store.GetChargingCostInPeriod(vehicleId, start, end);

            // Fuel cost (ICE)
            var fuelCost = _store.GetFuelCostInPeriod(vehicleId, start, end);

            // Maintenance cost
            var maintenanceCost = _store.GetMaintenanceCostInPeriod(vehicleId, start, end);

            return new MonthlyTransportationSummary
            {
                VehicleId = vehicleId,
                Year = year,
                Month = month,
                MilesDriven = milesDriven ?? 0,
                FuelCost = fuelCost,
                ChargingCost = chargingCost,
                MaintenanceCost = maintenanceCost,
                IsPartial = milesDriven == null
            };
        }

        /// <summary>
        /// Computes monthly summaries for all vehicles.
        /// </summary>
        public IReadOnlyCollection<MonthlyTransportationSummary> ComputeAllMonthlySummaries(int year, int month)
        {
            var results = new List<MonthlyTransportationSummary>();

            foreach (var vehicle in _store.GetAllVehicles())
            {
                results.Add(ComputeMonthlySummary(vehicle.Id, year, month));
            }

            return results.AsReadOnly();
        }

        /// <summary>
        /// Computes lifetime cost per mile for a vehicle.
        /// </summary>
        public decimal ComputeLifetimeCostPerMile(Guid vehicleId)
        {
            var mileage = _store.GetMileage(vehicleId);
            if (mileage.Count < 2)
                return 0;

            decimal milesDriven =
                mileage.Last().OdometerMiles - mileage.First().OdometerMiles;

            decimal chargingCost =
                _store.GetChargingCosts(vehicleId).Sum(c => c.Cost);

            decimal fuelCost =
                _store.GetFuelReceipts(vehicleId).Sum(r => r.Cost);

            decimal maintenanceCost =
                _store.GetMaintenanceInvoices(vehicleId).Sum(m => m.Cost);

            decimal totalCost = chargingCost + fuelCost + maintenanceCost;

            return milesDriven > 0 ? totalCost / milesDriven : 0;
        }
    }
}
