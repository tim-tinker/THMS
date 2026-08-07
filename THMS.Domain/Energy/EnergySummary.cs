using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class EnergySummary
    {
        public decimal ProducedKwh { get; set; }
        public decimal ConsumedKwh { get; set; }
        public decimal GridImportKwh { get; set; }
        public decimal GridExportKwh { get; set; }
        public decimal BatteryNetKwh { get; set; }
        public decimal NetImportKwh { get; set; }
        public decimal EvChargingKwh { get; set; }
    }
}
