using TrafficApiClient;

namespace PatternDetectionEngine;

public class DetectionEngine
{
    private double CheckDistance { get; }
    private const int InvertedHeadingCount = 50;

    public DetectionEngine(double checkDistance)
    {
        CheckDistance = checkDistance;
    }
    
    public bool AnalyseFlight(List<TrafficPosition> flightData)
    {
        // Remove unneeded traffic points
        var cleanedData = RemoveUnnecessaryPoints(flightData, "test");
        var patternResult = CheckForPattern(cleanedData);

        // This should return true if there is a holding pattern and false otherwise
        return patternResult;
    }

    private bool CheckForPattern(List<TrafficPosition> cleanedData)
    {
        var headingCount = cleanedData.Sum(
            point => cleanedData.Where(secondPoint => secondPoint.Heading != point.Heading)
                .Count(secondPoint => point.Heading == secondPoint.Heading + 180 % 360));
        return headingCount > InvertedHeadingCount;
    }

    public List<TrafficPosition> RemoveUnnecessaryPoints(List<TrafficPosition> flightData, string destAirport)
    {
        // Could use navdb to get lat and long for destination airport instead of using last point.

        var lastLat = flightData.Last().Lat;
        var lastLon = flightData.Last().Lon;

        return flightData.Where(f =>
            WithinDistance(f.Lat, lastLat) && WithinDistance(f.Lon, lastLon)).ToList();
    }

    private bool WithinDistance(double pointToCheck, double pointBoundary)
    {
        return pointToCheck > pointBoundary - CheckDistance && pointToCheck < pointBoundary + CheckDistance;
    }
} 