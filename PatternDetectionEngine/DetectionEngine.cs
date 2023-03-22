using TrafficApiClient;

namespace PatternDetectionEngine;

public class DetectionEngine
{
    private double CheckDistance { get; }

    public DetectionEngine(double checkDistance)
    {
        CheckDistance = checkDistance;
    }
    
    public bool AnalyseFlight(List<TrafficPosition> flightData)
    {
        // Remove unneeded traffic points
        var cleanedData = RemoveUnnecessaryPoints(flightData, "test");

        // This should return true if there is a holding pattern and false otherwise
        return false;
    }

    public List<TrafficPosition> RemoveUnnecessaryPoints(List<TrafficPosition> flightData, string destAirport)
    {
        // Could use navdb to get lat and long for destination airport instead of using last point.

        var lastLat = flightData.Last().Lat;
        var lastLon = flightData.Last().Lon;

        return flightData.Where(f =>
            WithinDistance(f.Lat, lastLat, CheckDistance) && WithinDistance(f.Lon, lastLon, CheckDistance)).ToList();
    }

    private static bool WithinDistance(double pointToCheck, double pointBoundary, double dist)
    {
        return pointToCheck > pointBoundary - dist && pointToCheck < pointBoundary + dist;
    }
} 