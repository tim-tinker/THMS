namespace THMS.Logic.Energy
{
    /// <summary>
    /// SPAN circuit energy is exported in 10-minute intervals timestamped
    /// at the start of each interval.
    /// </summary>
    public static class HomeCircuitInterval
    {
        public static readonly TimeSpan Length = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Start of the circuit interval that contains <paramref name="instant"/>.
        /// An instant that falls on a boundary belongs to the interval that starts then.
        /// </summary>
        public static DateTime StartContaining(DateTime instant)
        {
            var lengthTicks = Length.Ticks;
            var flooredTicks = instant.Ticks - (instant.Ticks % lengthTicks);
            return new DateTime(flooredTicks, instant.Kind);
        }
    }
}
