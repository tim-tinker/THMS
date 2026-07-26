namespace THMS.Domain.Transportation
{
    public class MpgeResult
    {
        public DateTime Date { get; set; }
        public decimal MilesDriven { get; set; }
        public decimal WhUsed { get; set; }

        /// <summary>
        /// Watt-hours per mile for the driving segment.
        /// </summary>
        public decimal WhPerMile => MilesDriven > 0 ? WhUsed / MilesDriven : 0;

        /// <summary>
        /// EPA MPGe rating for the driving segment.
        /// </summary>
        public decimal Mpge => WhPerMile > 0 ? 33700m / WhPerMile : 0;

        /// <summary>
        /// Effective battery capacity (Wh) calculated from the charging session
        /// that preceded this driving segment.
        /// </summary>
        public decimal EffectiveCapacityWh { get; set; }
    }
}
