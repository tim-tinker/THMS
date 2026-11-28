namespace THMS.Logic.ViewModels.Income
{
    public sealed class PayslipListRow
    {
        public Guid Id { get; init; }
        public DateTime PayDate { get; init; }
        public decimal Gross { get; init; }
        public decimal GrossYearToDate { get; init; }
        public decimal Traditional401kYearToDate { get; init; }
        public decimal Roth401kYearToDate { get; init; }
        public decimal HsaYearToDate { get; init; }
    }
}
