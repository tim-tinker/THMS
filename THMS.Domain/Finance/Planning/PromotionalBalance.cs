namespace THMS.Domain.Finance.Planning
{
    public class PromotionalBalance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Deadline { get; set; }
        public PromoType Type { get; set; }
    }
}
