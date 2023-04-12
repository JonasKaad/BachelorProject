using System.Diagnostics;
using System.Net.Http.Json;
using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using TrafficApiClient;

namespace PatternDetectionEngine;

public class DetectionEngine
{
    private double CheckDistance { get; }
    private const int InvertedHeadingCount = 2;
    private const int InvertedHeadingBuffer = 5;
    private const int AltitudeBuffer = 200;
    private readonly HttpClient _httpClient;

    public DetectionEngine(double checkDistance)
    {
        CheckDistance = checkDistance;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("http://localhost/", UriKind.Absolute);
    }
    
    public async Task<HoldingResult> AnalyseFlight(List<TrafficPosition> flightData)
    {
        var startTime = Stopwatch.StartNew();
        var cleanedData = await RemoveUnnecessaryPoints(flightData);
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
        Console.WriteLine(cleanedData.First().Lat);
        Console.WriteLine(cleanedData.First().Lon);
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

    public async Task<List<TrafficPosition>> RemoveUnnecessaryPoints(List<TrafficPosition> flightData)
    {
        // Could use navdb to get lat and long for destination airport instead of using last point.

        var lastLat = flightData.Last().Lat;
        var lastLon = flightData.Last().Lon;

        EAirport? dest = null;
        
        try
        {
            var airportResponse = await _httpClient.GetAsync($"NavDb/Airport?ICAO={flightData.Last().Dest}");
            
            if (airportResponse.IsSuccessStatusCode)
            {
                dest = await airportResponse.Content.ReadFromJsonAsync<EAirport>();
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e.InnerException);
        }

        var zeroedAltitude = 0.0;
        if (dest is not null)
            zeroedAltitude = dest.Elevation;

        

        return flightData.Where(f =>
            WithinDistance(f.Lat, lastLat) && WithinDistance(f.Lon, lastLon))
            .Where(p => p.Alt > zeroedAltitude + 7000).ToList();
    }

    private bool WithinDistance(double pointToCheck, double pointBoundary)
    {
        return pointToCheck > pointBoundary - CheckDistance && pointToCheck < pointBoundary + CheckDistance;
    }
} 