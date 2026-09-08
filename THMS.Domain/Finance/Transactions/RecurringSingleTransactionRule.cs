namespace THMS.Domain.Finance.Transactions
{
    public class RecurringSingleTransactionRule : BaseSingleAccountTransaction
    {
        public RecurrenceFrequency Frequency { get; set; }
        public DateTime NextOccurrence
        {
            get => Date;
            set => Date = value;
        }

        public DateTime? LastOccurrence { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsUserCreated { get; set; }

        // Optional loan-specific fields
        public bool IsFinalPaymentDifferent { get; set; }
        public decimal? FinalPaymentAmount { get; set; }
    }
}
