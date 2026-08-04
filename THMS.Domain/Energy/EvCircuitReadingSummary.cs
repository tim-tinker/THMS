using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class EvCircuitReadingSummary
    {
        public decimal GridKwh { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal BatteryKwh { get; set; }
        public decimal TotalKwh => GridKwh + SolarKwh + BatteryKwh;
    }
}
