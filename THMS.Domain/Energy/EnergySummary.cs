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
        public decimal BatteryChargeKwh { get; set; }
        public decimal BatteryDischargeKwh { get; set; }
        public decimal EvChargeKwh { get; set; }
        public decimal EvConsumption => ConsumedKwh == 0 ? 0 : EvChargeKwh / ConsumedKwh;
        public decimal NetImportKwh => GridImportKwh - GridExportKwh;
        public decimal GridDependence =>
            ConsumedKwh == 0 ? 0 : (GridImportKwh / ConsumedKwh) * 100;
        public decimal NetGridDependence =>
            ConsumedKwh == 0 ? 0 : ((GridImportKwh - GridExportKwh) / ConsumedKwh) * 100;
    }
}
