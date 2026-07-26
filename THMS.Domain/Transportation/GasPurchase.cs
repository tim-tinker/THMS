using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Transportation
{
    public class GasPurchase
    {
        public Guid Id { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Gallons { get; set; }
        public decimal FuelCost { get; set; }

        public string Station { get; set; }
    }
}
