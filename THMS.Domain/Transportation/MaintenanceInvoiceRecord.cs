namespace THMS.Domain.Transportation
{
    public class MaintenanceInvoiceRecord : BaseDomainModel
    {
        public Guid VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
        public string? Description { get; set; }
        public string? Vendor { get; set; }
    }
}
