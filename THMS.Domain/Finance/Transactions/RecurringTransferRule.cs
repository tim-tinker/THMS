namespace THMS.Domain.Finance.Transactions
{
    public class RecurringTransferRule : TransferTransaction
    {
        public RecurrenceFrequency Frequency { get; set; }
        public DateTime NextOccurrence 
        {
            get => Date;
            set => Date = value;
        }

        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsFinalPaymentDifferent { get; set; }
        public decimal? FinalPaymentAmount { get; set; }
    }
}
