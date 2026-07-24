namespace THMS.Domain.Transportation
{
    public class GasReceiptRecord : BaseDomainModel
    {
        public Guid VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Gallons { get; set; }
        public decimal Cost { get; set; }
        public string? Station { get; set; }
    }
}
