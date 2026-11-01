using System.ComponentModel;

namespace THMS.Logic.ViewModels
{
    public sealed class IceFillUpContribution
    {
        [DisplayName("Date")]
        public DateTime Date { get; init; }

        [DisplayName("Odometer")]
        public decimal OdometerMiles { get; init; }

        [DisplayName("Gallons")]
        public decimal Gallons { get; init; }

        [DisplayName("Cost")]
        public decimal Cost { get; init; }
    }
}
