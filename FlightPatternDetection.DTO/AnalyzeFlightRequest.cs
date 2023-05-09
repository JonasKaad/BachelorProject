using System.ComponentModel.DataAnnotations;

namespace FlightPatternDetection.DTO
{
    public class AnalyzeFlightRequest
    {
        [Required]
        public long FlightId { get; set; }
        public bool UseFallback { get; set; }
        public bool EnableDbCollection { get; set; } = true;
    }
}
