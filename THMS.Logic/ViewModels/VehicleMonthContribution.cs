using System.ComponentModel;

namespace THMS.Logic.ViewModels
{
    public sealed class VehicleMonthContribution
    {
        [DisplayName("Month")]
        public DateTime Month { get; init; }

        [DisplayName("MPG")]
        public decimal Mpg { get; init; }

        [DisplayName("Cost/Mile")]
        public decimal CostPerMile { get; init; }

        [DisplayName("Total Cost")]
        public decimal TotalCost { get; init; }

        [DisplayName("Total Miles")]
        public decimal TotalMiles { get; init; }

        [DisplayName("Gallons")]
        public decimal Gallons { get; init; }

        [Browsable(false)]
        public IReadOnlyList<EvChargeSessionContribution> ChargeSessions { get; init; } = [];

        [Browsable(false)]
        public IReadOnlyList<IceFillUpContribution> FillUps { get; init; } = [];
    }
}
