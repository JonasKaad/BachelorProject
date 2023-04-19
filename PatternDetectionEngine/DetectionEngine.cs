using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Services;
using PatternDetectionEngine.FlightFilters;
using TrafficApiClient;

namespace PatternDetectionEngine;

public class DetectionEngine
{
    private double CheckDistance { get; }
    private const int InvertedHeadingCount = 2;
    private const int InvertedHeadingBuffer = 5;
    private const int AltitudeBuffer = 200;
    private INavDbManager? _navDbManager;

    private List<IFlightFilter> FlightFilters { get; set; }

    public DetectionEngine(double checkDistance, INavDbManager navDbManager)
    {
        CheckDistance = checkDistance;
        _navDbManager = navDbManager;

        //FlightFilters
        FlightFilters = new List<IFlightFilter>()
        {
            new AircraftTypeFilter(),
            new ShortFlightFilter(),
        };
    }

    public HoldingResult AnalyseFlight(List<TrafficPosition> flightData)
    {
        if (flightData.Count == 0)
        {
            return new HoldingResult()
            {
                IsHolding = false,
                DetectionTime = TimeSpan.Zero,
            };
        }

        var startTime = Stopwatch.StartNew();
        if (FlightIsFiltered(flightData))
        {
            return new HoldingResult()
            {
                IsHolding = false,
                DetectionTime = startTime.Elapsed
            };
        }
        var cleanedData = RemoveUnnecessaryPoints(flightData);
        var patternResult = CheckForPattern(cleanedData);

        patternResult.DetectionTime = startTime.Elapsed;
        return patternResult;
    }

    private HoldingResult CheckForPattern(List<TrafficPosition> cleanedData)
    {
        TrafficPosition? firstInversionPoint = null;
        TrafficPosition? lastInversionPoint = null;
        var foundHoldings = new List<List<TrafficPosition>>();
        var cleanedDataCopy = new List<TrafficPosition>(cleanedData);
        const int pointsToTake = 10;
        List<TrafficPosition>? currentHolding = null;
        while (cleanedDataCopy.Count > 0)
        {
            var currentPoint = cleanedDataCopy.First();
            foreach (var nextPoint in cleanedDataCopy.Skip(1).Take(pointsToTake))
            {
                //Find the next holding
                if (IsInvertedHeading(currentPoint, nextPoint) && IsSameAltitude(currentPoint, nextPoint) && IsRecentEnough(currentPoint, nextPoint))
                {
                    if (currentHolding is null)
                    {
                        var lastPointInCurrentHeading = cleanedDataCopy.TakeWhile(x => x.Heading >= Math.Abs(currentPoint.Heading - InvertedHeadingBuffer * 2) && x.Heading <= Math.Abs(currentPoint.Heading + InvertedHeadingBuffer * 2)).Last();

                        currentHolding = new List<TrafficPosition>() {
                            lastPointInCurrentHeading,
                            nextPoint
                        };

                        firstInversionPoint ??= lastPointInCurrentHeading;
                        lastInversionPoint = nextPoint;
                    }
                    else
                    {
                        lastInversionPoint = nextPoint;
                        currentHolding.Add(nextPoint);
                    }
                    break;
                }
            }

            //Current holding, men vi fandt ingenting.
            if (currentHolding is not null && lastInversionPoint == currentPoint)
            {
                //Find the last part of current holding pattern
                foundHoldings.Add(currentHolding);
                currentHolding = null;
                lastInversionPoint = null;
            }

            if (lastInversionPoint is not null && cleanedDataCopy.Contains(lastInversionPoint))
            {
                cleanedDataCopy.RemoveRange(0, cleanedDataCopy.IndexOf(lastInversionPoint));
            }
            else
            {
                cleanedDataCopy.RemoveAt(0);
            }
        }

        //Filter any "holdings" with only 2 points (effectively filters u-turns)
        foundHoldings = foundHoldings.Where(x => x.Count > 2).ToList();

        //Did we find anything?
        if (!foundHoldings.Any())
        {
            return new();
        }

        //Holdings were found. Find the laps and stuff.

#if DEBUG
        Console.WriteLine("Copy paste to JavaScript console");
        foreach (var holding in foundHoldings)
        {
            Console.WriteLine("Found a holding pattern: ");
            foreach (var point in holding)
                Console.WriteLine($"addPoint({point.Lat.ToString(CultureInfo.InvariantCulture)}, {point.Lon.ToString(CultureInfo.InvariantCulture)});");
        }
#endif

        //TODO: Technically have support for more. But api DTO's does not. So for now, just treat as one. 
        var theHoldingPattern = foundHoldings.First();

        firstInversionPoint = theHoldingPattern.First();
        lastInversionPoint = theHoldingPattern.Last();

        var headingDiff = CalculateDirection(cleanedData, firstInversionPoint);
        if (headingDiff is null)
            return new();
        
        var fixPoint = FindHoldingFixPoint(cleanedData, firstInversionPoint, lastInversionPoint, theHoldingPattern) ??
                    new EWayPoint(
                        theHoldingPattern.First().Lat,
                        theHoldingPattern.First().Lon, 
                        "FIX",
                        -1)
                    {
                        Name = "FIXPOINT"
                    };

        int laps = (int)Math.Ceiling(theHoldingPattern.Count / 2d);

        var holdingPattern = new HoldingResult()
        {
            IsHolding = foundHoldings.Any(),
            Direction = headingDiff > 0 ? HoldingDirection.Left : HoldingDirection.Right,
            Altitude = firstInversionPoint.Alt,
            Laps = laps,
            FixPoint = fixPoint
        };
        return holdingPattern;
    }

    private EWayPoint? FindHoldingFixPoint(List<TrafficPosition> cleanedData, TrafficPosition firstInversionPoint, TrafficPosition lastInversionPoint, List<TrafficPosition> holdingPoints)
    {
        if (_navDbManager is null)
            return null;
        var firstIndex = cleanedData.IndexOf(firstInversionPoint);
        var lastIndex = cleanedData.IndexOf(lastInversionPoint);
        var holdingPointsInHolding = cleanedData.Skip(firstIndex).Take(lastIndex);
        var trafficPositions = holdingPointsInHolding.ToList();
        EWayPoint? possibleFixPoint = null;
        const double pointBuffer = 0.25;
        var lonDiff = 180.0;
        var latDiff = 90.0;

        var waypointsAroundHolding = _navDbManager.Waypoints.Where(waypoint => 
            IsCloseEnough(waypoint, firstInversionPoint, pointBuffer)
        ).ToList();
        
        foreach (var point in trafficPositions)
        {
            var closePoints = waypointsAroundHolding.Where(p => 
                holdingPoints.Any(h => IsCloseEnough(p, h, 0.025))
            ).ToList();
            if (closePoints.Any())
                return closePoints.First();

            if (!waypointsAroundHolding.Any())
                continue;
            foreach (var fixPoint in waypointsAroundHolding)
            {
                var pointLonDiff = Math.Abs(fixPoint.Longitude - point.Lon);
                var pointLatDiff = Math.Abs(fixPoint.Latitude - point.Lat);
                if (pointLatDiff + pointLonDiff > lonDiff + latDiff)
                    continue;                                                
                possibleFixPoint = fixPoint;                                
                lonDiff = pointLonDiff;
                latDiff = pointLatDiff;
            }
        }

        return possibleFixPoint;
    }

    private bool IsCloseEnough(EWayPoint waypoint, TrafficPosition point, double buffer)
    {
        return waypoint.Longitude >= point.Lon - buffer && waypoint.Longitude <= point.Lon + buffer &&
               waypoint.Latitude >= point.Lat - buffer && waypoint.Latitude <= point.Lat + buffer;
    }

    private bool IsRecentEnough(TrafficPosition point, TrafficPosition second)
    {
        return second.Clock - point.Clock < 350 && second.Clock - point.Clock > 90;
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
        return point.Heading < (second.Heading + (180 + InvertedHeadingBuffer)) % 360 && point.Heading > (second.Heading + (180 - InvertedHeadingBuffer)) % 360;
    }

    private bool IsSameAltitude(TrafficPosition point, TrafficPosition second)
    {
        return point.Alt > second.Alt - AltitudeBuffer && point.Alt < second.Alt + AltitudeBuffer;
    }

    public bool FlightIsFiltered(List<TrafficPosition> flightData)
    {
        foreach (var filter in FlightFilters)
        {
            if (filter.ShouldFilter(flightData))
            {
#if DEBUG
                Console.WriteLine($"[DEBUG]: Detection engine filtered flight with filter: {filter.GetType().Name}");
#endif
                return true;
            }
        }
        return false;
    }

    public List<TrafficPosition> RemoveUnnecessaryPoints(List<TrafficPosition> flightData)
    {
        // Could use navdb to get lat and long for destination airport instead of using last point.

        var lastLat = flightData.Last().Lat;
        var lastLon = flightData.Last().Lon;

        EAirport? dest = null;

        if (_navDbManager is not null)
        {
            dest = _navDbManager.Airports.FirstOrDefault(x => x.ICAO == flightData.Last().Dest);
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