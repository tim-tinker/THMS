namespace THMS.Logic.Energy
{
    public static class TimeBucket
    {
        /// <summary>
        /// Returns the half-hour bucket key for the given timestamp.
        /// Buckets:
        ///   00–14  → HH:00
        ///   15–44  → HH:30
        ///   45–59  → (HH+1):00 (with correct day/month/year rollover)
        /// </summary>
        public static DateTime GetHalfHour(DateTime ts)
        {
            int minute = ts.Minute;

            if (minute < 15)
            {
                // HH:00
                return new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, 0, 0);
            }
            else if (minute < 45)
            {
                // HH:30
                return new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, 30, 0);
            }
            else
            {
                // (HH+1):00 — safe rollover using AddHours(1)
                var next = ts.AddHours(1);
                return new DateTime(next.Year, next.Month, next.Day, next.Hour, 0, 0);
            }
        }
    }
}
