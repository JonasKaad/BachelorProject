using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightPatternDetection.DTO
{
    public class HoldingResult
    {
        public bool IsHolding { get; set; }
        public TimeSpan DetectionTime { get; set; }
    }
}
