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
        private readonly IVehicleDataStore _store;

        public TransportationAnalyticsEngine(IVehicleDataStore store)
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
                MilesDriven = milesDriven,
                FuelCost = fuelCost,
                ChargingCost = chargingCost,
                MaintenanceCost = maintenanceCost,
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
            decimal costPerMile = 0;

            var start = DateTime.MinValue;
            var end = DateTime.Now;

            decimal milesDriven = _store.GetMilesDrivenInPeriod(vehicleId, start, end);
            if (milesDriven > 0)
            {
                decimal chargingCost =
                    _store.GetChargingCosts(vehicleId, start, end).Sum(c => c.Cost);

                decimal fuelCost =
                    _store.GetFuelReceipts(vehicleId, start, end).Sum(r => r.FuelCost);

                decimal maintenanceCost =
                    _store.GetMaintenanceInvoices(vehicleId, start, end).Sum(m => m.Cost);

                decimal totalCost = chargingCost + fuelCost + maintenanceCost;

                costPerMile = totalCost / milesDriven;
            }
            
            return costPerMile;
        }
    }
}
