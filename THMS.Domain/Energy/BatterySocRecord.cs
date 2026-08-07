using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Energy
{
    public class BatterySocRecord
    {
        public DateTime Timestamp { get; set; }
        public decimal SocPercent { get; set; }
    }
}
