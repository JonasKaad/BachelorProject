using FlightPatternDetection.DTO;
using TrafficApiClient;

namespace PatternDetectionEngine
{
    public interface IDetectionEngine
    {
        HoldingResult AnalyseFlight(List<TrafficPosition> flightData);
        bool FlightIsFiltered(List<TrafficPosition> flightData);
        List<TrafficPosition> RemoveUnnecessaryPoints(List<TrafficPosition> flightData);
    }
}