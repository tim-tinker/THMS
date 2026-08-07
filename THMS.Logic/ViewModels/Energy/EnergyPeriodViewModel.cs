using System;
using System.Collections.Generic;
using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class EnergyPeriodViewModel
    {
        // ---------------------------------------------------------
        // Metadata
        // ---------------------------------------------------------
        public DateTime Start { get; }
        public DateTime End { get; }

        // ---------------------------------------------------------
        // Time-series data (daily or monthly)
        // ---------------------------------------------------------
        public IReadOnlyList<EnergyPeriodRecord> Records { get; }

        // ---------------------------------------------------------
        // Totals for the entire period
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
        public EnergyPeriodViewModel(EnergyPeriod period)
        {
            Start = period.Start;
            End = period.End;

            Records = period.Records;

            SolarKwh = period.SolarKwh;
            BatteryChargeKwh = period.BatteryChargeKwh;
            BatteryDischargeKwh = period.BatteryDischargeKwh;
            GridImportKwh = period.GridImportKwh;
            GridExportKwh = period.GridExportKwh;
            HomeConsumptionKwh = period.HomeConsumptionKwh;
            EvChargingKwh = period.EvChargingKwh;
        }

        // Empty constructor for Custom before user selects a range
        public EnergyPeriodViewModel()
        {
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            Records = Array.Empty<EnergyPeriodRecord>();
        }
    }
}
