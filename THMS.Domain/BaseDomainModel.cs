namespace THMS.Domain
{
    public abstract class BaseDomainModel
    {
        /// <summary>
        /// Unique identifier for the domain object.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Display name for UI lists and dashboards.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional: common monthly breakdown pattern.
        /// </summary>
        public virtual IReadOnlyList<MonthlyValue> MonthlyBreakdown => Array.Empty<MonthlyValue>();

        /// <summary>
        /// Optional: common annual cost pattern.
        /// </summary>
        public virtual decimal AnnualCost => MonthlyBreakdown?.Sum(m => m.Amount) ?? 0m;

        /// <summary>
        /// Optional: common description field.
        /// </summary>
        public virtual string Description => Name;

        /// <summary>
        /// Allows domain objects to refresh internal calculations.
        /// </summary>
        public virtual void Recalculate()
        {
            // Default: nothing to recalc
        }
    }
}
