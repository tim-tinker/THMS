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

        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Optional loan-specific fields
        public bool IsFinalPaymentDifferent { get; set; }
        public decimal? FinalPaymentAmount { get; set; }
    }
}
