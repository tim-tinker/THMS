namespace THMS.Domain.Finance.Accounts
{
    public class LoanAccount : Account
    {
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
        public DateTime StartDate { get; set; }
        public PaymentScheduleType ScheduleType { get; set; }
    }

    public enum PaymentScheduleType
    {
        Fixed,
        Step,
        Variable

    }
}
