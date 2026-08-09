using System;
using System.Collections.Generic;
using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class EnergyDayViewModel
    {
        // ---------------------------------------------------------
        // Basic metadata
        // ---------------------------------------------------------
        public DateTime Date { get; }

        // ---------------------------------------------------------
        // Hourly time-series data (for charts)
        // ---------------------------------------------------------
        public IReadOnlyList<EnergyIntervalRecord> Intervals { get; }

        // ---------------------------------------------------------
        // Daily totals (for summary + breakdown)
        // ---------------------------------------------------------
        public decimal SolarKwh { get; }
        public decimal BatteryChargeKwh { get; }
        public decimal BatteryDischargeKwh { get; }
        public decimal GridImportKwh { get; }
        public decimal GridExportKwh { get; }
        public decimal HomeConsumptionKwh { get; }
        public decimal EvChargingKwh { get; }

        // ---------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------
        public EnergyDayViewModel(EnergyDay day)
        {
            Date = day.Date;

            Intervals = day.Intervals;

            SolarKwh = day.SolarKwh;
            BatteryChargeKwh = day.BatteryChargeKwh;
            BatteryDischargeKwh = day.BatteryDischargeKwh;
            GridImportKwh = day.GridImportKwh;
            GridExportKwh = day.GridExportKwh;
            HomeConsumptionKwh = day.HomeConsumptionKwh;
            EvChargingKwh = day.EvChargingKwh;
        }
    }
}
