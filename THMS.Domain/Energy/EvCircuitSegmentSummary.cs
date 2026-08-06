using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class EvCircuitSegmentSummary
    {
        public Guid SessionId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int SegmentCount { get; set; }
        public decimal GridKwh { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal BatteryKwh { get; set; }
        public decimal TotalKwh { get; set; }

    }
}
