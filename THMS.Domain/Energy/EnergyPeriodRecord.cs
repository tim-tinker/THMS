using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class EnergyPeriodRecord
    {
        public DateTime Date { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal BatteryChargeKwh { get; set; }
        public decimal BatteryDischargeKwh { get; set; }
        public decimal GridImportKwh { get; set; }
        public decimal GridExportKwh { get; set; }
        public decimal EvChargeKwh { get; set; }
        public decimal HomeConsumptionKwh { get; set; }
        public decimal EvChargingKwh { get; set; }
    }
}
