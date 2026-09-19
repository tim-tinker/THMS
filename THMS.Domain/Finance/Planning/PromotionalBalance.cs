namespace THMS.Domain.Finance.Planning
{
    public class PromotionalBalance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public DateTime DateAcquired { get; set; }
        public decimal InitialAmount { get; set; }
        public decimal CurrentBalance { get; set; }

        /// <summary>Remaining promotional balance. Alias of <see cref="CurrentBalance"/>.</summary>
        public decimal Amount
        {
            get => CurrentBalance;
            set => CurrentBalance = value;
        }

        public DateTime Deadline { get; set; }
        public PromoType Type { get; set; }
    }
}
