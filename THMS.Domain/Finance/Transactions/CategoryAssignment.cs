namespace THMS.Domain.Finance.Transactions
{
    public class CategoryAssignment
    {
        public string NormalizedDescription { get; set; } = "";
        public Guid CategoryId { get; set; }
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}
