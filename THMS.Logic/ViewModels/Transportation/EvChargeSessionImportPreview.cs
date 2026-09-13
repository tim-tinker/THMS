namespace THMS.Logic.ViewModels.Transportation
{
    public class EvChargeSessionImportPreview
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal OdometerMiles { get; set; }
        public decimal StartSoc { get; set; }
        public decimal EndSoc { get; set; }
        public decimal KwhAdded { get; set; }
        public decimal LastOdometer { get; set; }
        public decimal LastSoc { get; set; }
        public decimal KwhDrawn { get; set; }
        public decimal SessionCost { get; set; }
        public bool IsHomeCharge { get; set; }
        public string ChargeType => IsHomeCharge ? "Home" : "Commercial";
        public string Charger { get; set; } = "";
        public string VehicleName { get; set; } = "";
        public Guid VehicleId { get; set; }
    }
}
