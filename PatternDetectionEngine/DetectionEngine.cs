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

        // This should return true if there is a holding pattern and false otherwise
        patternResult.DetectionTime = startTime.Elapsed;
        return patternResult;
    }

    private HoldingResult CheckForPattern(List<TrafficPosition> cleanedData)
    {
        var headingCount = cleanedData.Sum(
            point => cleanedData.Where(secondPoint => secondPoint.Heading != point.Heading)
                .Count(secondPoint => point.Heading == secondPoint.Heading + 180 % 360));
        TrafficPosition? firstInversionPoint = null;
        var manualHeadCount = 0;
        foreach (var point in cleanedData)
        {
            foreach (var second in cleanedData)
            {
                if(second.Heading == point.Heading)
                    continue;
                if (IsInvertedHeading(point, second) && IsSameAltitude(point, second))
                {
                    manualHeadCount++;
                    firstInversionPoint ??= point;
                    break;
                }
            }
        }

        if (firstInversionPoint is null)
            return new()
            {
                IsHolding = false
            };
        var secondHolding = cleanedData.SkipWhile(p => p != firstInversionPoint).SkipWhile(p => p.Heading == firstInversionPoint.Heading)
            .Take(1).First();
        var headingDiff = firstInversionPoint.Heading - secondHolding.Heading; // Positive: Right, Negative: Left
        var holdingPattern = new HoldingResult()
        {
            IsHolding = manualHeadCount > InvertedHeadingCount,
            Direction = headingDiff > 0 ? HoldingDirection.Left : HoldingDirection.Right,
            Altitude = firstInversionPoint.Alt,
            Laps = manualHeadCount
        };
        return holdingPattern;
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
            var airportResponse = await _httpClient.GetAsync($"NavDb/Airport?ICAO={flightData.Last().Airport}");
            
            if (airportResponse.IsSuccessStatusCode)
            {
                dest = await airportResponse.Content.ReadFromJsonAsync<EAirport>();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        var zeroedAltitude = 0.0;
        if (dest is not null)
            zeroedAltitude = dest.Elevation;
        

        return flightData.Where(f =>
            WithinDistance(f.Lat, lastLat) && WithinDistance(f.Lon, lastLon))
            .Where(p => p.Alt > zeroedAltitude + 1000).ToList();
    }

    private bool WithinDistance(double pointToCheck, double pointBoundary)
    {
        return pointToCheck > pointBoundary - CheckDistance && pointToCheck < pointBoundary + CheckDistance;
    }
} 