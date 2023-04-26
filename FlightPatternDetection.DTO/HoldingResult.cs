using FlightPatternDetection.DTO.NavDBEntities;

namespace FlightPatternDetection.DTO
{
    public class HoldingResult
    {
        public bool IsHolding { get; set; }
        public TimeSpan DetectionTime { get; set; }
        public HoldingDirection Direction { get; set; }
        public int Altitude { get; set; }
        public int Laps { get; set; }
        public EWayPoint FixPoint { get; set; }
    }
}
