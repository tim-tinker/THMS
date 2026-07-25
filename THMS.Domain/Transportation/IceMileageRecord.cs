namespace THMS.Domain.Transportation
{
    /// <summary>
    /// Represents a mileage event for an ICE vehicle,
    /// typically recorded at fuel fill-ups.
    /// </summary>
    public class IceMileageRecord : MileageRecordBase
    {
        /// <summary>
        /// Gallons of fuel added at this fill-up.
        /// </summary>
        public decimal GallonsAdded { get; set; }

        /// <summary>
        /// Indicates whether this was a full fill-up.
        /// Full fill-ups allow accurate MPG calculations.
        /// </summary>
        public bool IsFullFillUp { get; set; }

        /// <summary>
        /// Cost of the fuel purchased.
        /// </summary>
        public decimal FuelCost { get; set; }
    }
}
