namespace THMS.Domain.Transportation
{
    public abstract class MileageRecordBase : BaseDomainModel
    {
        public Guid VehicleId { get; set; }
        public DateTime EndTime { get; set; }
        public decimal OdometerMiles { get; set; }
    }
}
