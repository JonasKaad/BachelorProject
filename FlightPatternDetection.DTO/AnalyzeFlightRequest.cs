using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightPatternDetection.DTO
{
    public class AnalyzeFlightRequest
    {
        [Required]
        public long FlightId { get; set; }
        public bool UseFallback { get; set; }
    }
}
