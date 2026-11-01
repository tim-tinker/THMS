using System.ComponentModel;

namespace THMS.Logic.ViewModels
{
    public sealed class EvChargeSessionContribution
    {
        [DisplayName("End")]
        public DateTime EndTime { get; init; }

        [DisplayName("MPG")]
        public decimal Mpg { get; init; }

        [DisplayName("Cost/Mile")]
        public decimal CostPerMile { get; init; }

        [DisplayName("Home")]
        public bool IsHome { get; init; }

        [DisplayName("Cost")]
        public decimal Cost { get; init; }

        [DisplayName("Miles")]
        public decimal Miles { get; init; }

        [DisplayName("Gallons")]
        public decimal Gallons { get; init; }
    }
}
