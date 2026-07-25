using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Domain;
using THMS.Domain.Energy;
using THMS.Logic.ViewModels;

namespace THMS.Logic.ViewModels.Energy
{
    public class EnergyDashboardViewModel : BaseDashboardViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // This is populated from outside (for now)
        public List<EnergyAttributionResult> EnergyData { get; set; } = new();

        public decimal TotalSolarWh { get; private set; }
        public decimal TotalGridWh { get; private set; }
        public decimal TotalBatteryWh { get; private set; }
        public decimal TotalEvChargingWh { get; private set; }

        public EnergyDashboardViewModel()
        {
            var now = DateTime.Now;
            StartDate = new DateTime(now.Year, now.Month, 1);
            EndDate = StartDate.AddMonths(1).AddDays(-1);
        }

        public override void Initialize()
        {
            // If EnergyData is already populated, just compute totals.
            ComputeTotals();
        }

        public void Refresh()
        {
            // Caller is responsible for updating EnergyData before calling this.
            ComputeTotals();
        }

        private void ComputeTotals()
        {
            var filtered = EnergyData
                .Where(e => e.Timestamp >= StartDate &&
                            e.Timestamp <= EndDate)
                .ToList();

            TotalSolarWh = filtered.Sum(e => e.SolarWh);
            TotalGridWh = filtered.Sum(e => e.GridWh);
            TotalBatteryWh = filtered.Sum(e => e.BatteryWh);
            TotalEvChargingWh = filtered.Sum(e => e.EvChargingWh);
        }
    }
}
