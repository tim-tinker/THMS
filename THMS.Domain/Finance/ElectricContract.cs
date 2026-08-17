namespace THMS.Domain.Finance
{
    public class ElectricContract
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal BaseEnergyCharge { get; set; }       // $/month
        public decimal EnergyChargeRate { get; set; }       // $/kWh
        public decimal BaseDeliveryCharge { get; set; }     // $/month
        public decimal DeliveryChargeRate { get; set; }     // $/kWh
        public decimal ExportCreditRate { get; set; }       // $/kWh
    }
}
