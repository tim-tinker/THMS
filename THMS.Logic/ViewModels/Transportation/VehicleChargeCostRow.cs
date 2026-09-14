using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public sealed class VehicleChargeCostRow
    {
        public DateTime StartTime { get; init; }
        public decimal MilesDriven { get; init; }
        public decimal Cost { get; init; }
        public decimal CostPerMile { get; init; }

        public static VehicleChargeCostRow FromSession(BaseEvChargeSession session)
        {
            var miles = session.OdometerMiles > session.LastOdometer
                ? session.OdometerMiles - session.LastOdometer
                : 0m;
            var cost = session.SessionCost;
            return new VehicleChargeCostRow
            {
                StartTime = session.StartTime,
                MilesDriven = miles,
                Cost = cost,
                CostPerMile = miles > 0 ? cost / miles : 0m
            };
        }
    }
}
