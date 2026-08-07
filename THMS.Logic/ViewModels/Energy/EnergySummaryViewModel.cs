using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class EnergySummaryViewModel
    {
        public decimal ProducedKwh { get; }
        public decimal ConsumedKwh { get; }
        public decimal GridImportKwh { get; }
        public decimal GridExportKwh { get; }
        public decimal BatteryNetKwh { get; }
        public decimal NetImportKwh { get; }
        public decimal EvChargingKwh { get; }

        public EnergySummaryViewModel(EnergySummary summary)
        {
            ProducedKwh = summary.ProducedKwh;
            ConsumedKwh = summary.ConsumedKwh;
            GridImportKwh = summary.GridImportKwh;
            GridExportKwh = summary.GridExportKwh;
            BatteryNetKwh = summary.BatteryNetKwh;
            NetImportKwh = summary.NetImportKwh;
            EvChargingKwh = summary.EvChargingKwh;
        }
    }
}
