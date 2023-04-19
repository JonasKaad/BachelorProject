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
            new GliderFilter(),
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
        var manualHeadCount = 0;
        var foundHoldings = new List<List<TrafficPosition>>();
        var cleanedDataCopy = new List<TrafficPosition>(cleanedData);
        //foreach (var point in cleanedData)
        //{
        //    foreach (var second in cleanedData)
        //    {
        //        if (second.Heading == point.Heading)
        //            continue;
        //        if (IsInvertedHeading(point, second) && IsSameAltitude(point, second) && IsRecentEnough(point, second))
        //        {
        //            manualHeadCount++;
        //            points.Add(points.Contains(point) ? second : point);
        //            firstInversionPoint ??= point;
        //            lastInversionPoint = point;
        //            break;
        //        }
        //    }
        //}

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
                        manualHeadCount++;
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

        //Debug print
        Console.WriteLine("Copy paste to JavaScript console");
        foreach (var d in foundHoldings)
        {
            Console.WriteLine("Found a holding pattern: ");
            foreach (var p in d)
                Console.WriteLine($"addPoint({p.Lat.ToString(CultureInfo.InvariantCulture)}, {p.Lon.ToString(CultureInfo.InvariantCulture)});");
        }


        //TODO: Technically have support for more. But api DTO's does not. So for now, just treat as one. 
        var theHoldingPattern = foundHoldings.First();
        
        firstInversionPoint = theHoldingPattern.First();
        lastInversionPoint = theHoldingPattern.Last();

        var headingDiff = CalculateDirection(cleanedData, firstInversionPoint);
        if (headingDiff is null)
            return new();

        //var laps = CalculateLaps(cleanedData, firstInversionPoint, lastInversionPoint);
        int laps = (int)Math.Ceiling(theHoldingPattern.Count / 2d);
        
        //Probably just a star that goes in a circle.
        //if (laps <= 1)
        //{
        //    return new()
        //    {
        //        IsHolding = false
        //    };
        //}

        var holdingPattern = new HoldingResult()
        {
            IsHolding = foundHoldings.Any(),
            Direction = headingDiff > 0 ? HoldingDirection.Left : HoldingDirection.Right,
            Altitude = firstInversionPoint.Alt,
            Laps = laps
        };
        return holdingPattern;
    }

    private bool IsRecentEnough(TrafficPosition point, TrafficPosition second)
    {
        return second.Clock - point.Clock < 350 && second.Clock - point.Clock > 90;
    }

    private int CalculateLaps(List<TrafficPosition> cleanedData, TrafficPosition firstInversionPoint, TrafficPosition lastInversionPoint)
    {
        // Victors old method
        return (int)Math.Round((lastInversionPoint.Clock - firstInversionPoint.Clock) / 60.0);

        //var firstIndex = cleanedData.IndexOf(firstInversionPoint);
        //var lastIndex = cleanedData.IndexOf(firstInversionPoint);
        //var pointsInHoldingPattern = cleanedData.GetRange(firstIndex, lastIndex - firstIndex);
        //if (pointsInHoldingPattern.Count == 0)
        //{
        //    return 0;
        //}

        //int laps = 0;

        //var pairwisePointsInHoldingPattern = pointsInHoldingPattern.Zip(pointsInHoldingPattern.Skip(1), (a, b) => Tuple.Create(a, b));
        //var currentHeading = 1;
        //foreach (var (a, b) in pairwisePointsInHoldingPattern)
        //{

        //}



        //return laps;
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