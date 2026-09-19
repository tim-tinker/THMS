namespace THMS.Logic.ViewModels.Finance
{
    public class UnifiedAccountView
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Institution { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string AccountType { get; set; } = "";
        public string WebsiteUrl { get; set; } = "";
        public DateTime? StatementDate { get; set; }
        public decimal? StatementBalance { get; set; }
        public DateTime? AsOfDate { get; set; }

        // UI-only fields
        public decimal? Balance { get; set; }
        public decimal? AmountDue { get; set; }
        public decimal? BankCreditAvailable { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal? APR { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
