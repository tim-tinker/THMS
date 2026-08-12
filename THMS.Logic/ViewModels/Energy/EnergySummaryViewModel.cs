using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class EnergySummaryViewModel
    {
        public decimal ProducedKwh { get; }
        public decimal ConsumedKwh { get; }
        public decimal GridImportKwh { get; }
        public decimal GridExportKwh { get; }
        public decimal NetImportKwh { get; }
        public decimal GridDependence { get; }
        public decimal NetGridDependence { get; }
        public decimal BatteryChargeKwh { get; }
        public decimal BatteryDischargeKwh { get; }
        public decimal EvChargeKwh { get; }
        public decimal EvConsumption { get; }

        public EnergySummaryViewModel(EnergySummary summary)
        {
            ProducedKwh = summary.ProducedKwh;
            ConsumedKwh = summary.ConsumedKwh;
            GridImportKwh = summary.GridImportKwh;
            GridExportKwh = summary.GridExportKwh;
            NetImportKwh = summary.NetImportKwh;
            GridDependence = summary.GridDependence;
            NetGridDependence = summary.NetGridDependence;
            BatteryChargeKwh = summary.BatteryChargeKwh;
            BatteryDischargeKwh = summary.BatteryDischargeKwh;
            EvChargeKwh = summary.EvChargeKwh;
            EvConsumption = summary.EvConsumption;
        }
    }
}
