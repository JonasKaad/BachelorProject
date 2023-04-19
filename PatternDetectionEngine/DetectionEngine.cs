using System.Diagnostics;
using System.Net.Http.Json;
using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Services;
using TrafficApiClient;

namespace PatternDetectionEngine;

public class DetectionEngine
{
    private double CheckDistance { get; }
    private const int InvertedHeadingCount = 2;
    private const int InvertedHeadingBuffer = 5;
    private const int AltitudeBuffer = 200;
    private INavDbManager? _navDbManager;

    public DetectionEngine(double checkDistance, INavDbManager navDbManager)
    {
        CheckDistance = checkDistance;
        _navDbManager = navDbManager;
    }
    
    public HoldingResult AnalyseFlight(List<TrafficPosition> flightData)
    {
        var startTime = Stopwatch.StartNew();
        var cleanedData = RemoveUnnecessaryPoints(flightData);
        var patternResult = CheckForPattern(cleanedData);
        
        patternResult.DetectionTime = startTime.Elapsed;
        return patternResult;
    }

    private HoldingResult CheckForPattern(List<TrafficPosition> cleanedData)
    {
        TrafficPosition? firstInversionPoint = null;
        TrafficPosition? lastInversionPoint = null;
        var manualHeadCount = 0;
        var points = new List<TrafficPosition>();
        foreach (var point in cleanedData)
        {
            foreach (var second in cleanedData)
            {
                if(second.Heading == point.Heading)
                    continue;
                if (IsInvertedHeading(point, second) && IsSameAltitude(point, second) && IsRecentEnough(point, second))
                {
                    manualHeadCount++;
                    points.Add(points.Contains(point) ? second : point);
                    firstInversionPoint ??= point;
                    lastInversionPoint = point;
                    break;
                }
            }
        }

        if (firstInversionPoint is null)
            return new()
            {
                IsHolding = false
            };
        var headingDiff = CalculateDirection(cleanedData, firstInversionPoint);
        if (headingDiff is null)
            return new();

        var laps = CalculateLaps(cleanedData, firstInversionPoint, lastInversionPoint);
        var holdingPattern = new HoldingResult()
        {
            IsHolding = manualHeadCount > InvertedHeadingCount,
            Direction = headingDiff > 0 ? HoldingDirection.Left : HoldingDirection.Right,
            Altitude = firstInversionPoint.Alt,
            Laps = laps
        };
        return holdingPattern;
    }

    private bool IsRecentEnough(TrafficPosition point, TrafficPosition second)
    {
        return second.Clock - point.Clock < 250 && second.Clock - point.Clock > 90;
    }

    private int CalculateLaps(List<TrafficPosition> cleanedData, TrafficPosition firstInversionPoint, TrafficPosition lastInversionPoint)
    {
        return (int)Math.Round((lastInversionPoint.Clock - firstInversionPoint.Clock) / 60.0);
    }

    private static double? CalculateDirection(List<TrafficPosition> cleanedData, TrafficPosition firstInversionPoint)
    {
        var secondHolding = cleanedData.SkipWhile(p => p != firstInversionPoint)
            .SkipWhile(p => p.Heading == firstInversionPoint.Heading)
            .Take(1).FirstOrDefault();
        
        if (secondHolding is null)
        {
            return null;
        }

        return firstInversionPoint.Heading - secondHolding.Heading; // Positive: Right, Negative: Left
    }

    private bool IsInvertedHeading(TrafficPosition point, TrafficPosition second)
    {
        return point.Heading < second.Heading + (180 + InvertedHeadingBuffer) % 360 && point.Heading > second.Heading + (180 - InvertedHeadingBuffer) % 360;
    }

    private bool IsSameAltitude(TrafficPosition point, TrafficPosition second)
    {
        return point.Alt > second.Alt - AltitudeBuffer && point.Alt < second.Alt + AltitudeBuffer;
    }

    public List<TrafficPosition> RemoveUnnecessaryPoints(List<TrafficPosition> flightData)
    {
        // Could use navdb to get lat and long for destination airport instead of using last point.

        var lastLat = flightData.Last().Lat;
        var lastLon = flightData.Last().Lon;

        EAirport? dest = null;

        if (_navDbManager is not null)
        {
            dest = _navDbManager.Airports.FirstOrDefault(x=>  x.ICAO == flightData.Last().Dest);
            dest ??= _navDbManager.Airports.FirstOrDefault(x => 
                x.Longitude >= lastLon + 0.125 && x.Longitude <= lastLon - 0.125 
                                               && x.Latitude >= lastLat + 0.125 && x.Latitude <= lastLat - 0.125
            );

        }

        var zeroedAltitude = 0.0;
        if (dest is not null)
            zeroedAltitude = dest.Elevation;

        

        return flightData.Where(f =>
            WithinDistance(f.Lat, lastLat) && WithinDistance(f.Lon, lastLon))
            .Where(p => p.Alt > zeroedAltitude + 5000).ToList();
    }

    private bool WithinDistance(double pointToCheck, double pointBoundary)
    {
        return pointToCheck > pointBoundary - CheckDistance && pointToCheck < pointBoundary + CheckDistance;
    }
} 