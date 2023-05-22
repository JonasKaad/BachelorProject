using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Services;
using PatternDetectionEngine.FlightFilters;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using TrafficApiClient;

namespace PatternDetectionEngine;

public class DetectionEngine
{
    private double CheckDistance { get; }
    private const int InvertedHeadingBuffer = 5;
    private const int AltitudeBuffer = 500;
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
        const int PointsToTake = 12;
        const double ApproximateMaximumRadiusOfHoldingPattern = 20; //20 Nautical Miles (~37 km)
        List<TrafficPosition>? currentHolding = null;
        while (cleanedDataCopy.Count > 0)
        {
            var currentPoint = cleanedDataCopy.First();
            foreach (var nextPoint in cleanedDataCopy.Skip(1).Take(PointsToTake))
            {
                //Find the next holding
                if (IsInvertedHeading(currentPoint, nextPoint)
                    && IsSameAltitude(currentPoint, nextPoint)
                    && IsRecentEnough(currentPoint, nextPoint)
                    && IsDistantEnough(currentPoint, nextPoint, 2.5)
                    && IsCloseEnough(currentPoint, nextPoint, ApproximateMaximumRadiusOfHoldingPattern)
                    )
                {
                    if (currentHolding is null)
                    {
                        var pointsInCurrentHeading = cleanedDataCopy.TakeWhile(x => IsSameDirection(currentPoint.Heading, x.Heading, InvertedHeadingBuffer * 2))
                                                    .ToList();

                        var lastPointInCurrentHeading = pointsInCurrentHeading.Last();
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

        if (foundHoldings.Count > 1)
        {
            //If we found more and just one, we check if they're close to each other. If they are, we merge them.
            var newFoundHoldings = new List<List<TrafficPosition>>()
            {
                new(foundHoldings.First())
                {}
            };

            foreach (var holding in foundHoldings.Skip(1))
            {
                var lastNewFoundHoldingPoint = newFoundHoldings.Last().Last();
                if (IsCloseEnough(lastNewFoundHoldingPoint, holding.Last(), ApproximateMaximumRadiusOfHoldingPattern / 2))
                {
                    //The last holding's last point, is close to this holding's first point.
                    //Thus we figure they must be part of the same holding, so we merge them
                    newFoundHoldings.Last().AddRange(holding);
                }
                else
                {
                    //The last holding, and this holding are far apart. We assume they're different
                    newFoundHoldings.Add(holding);
                }
            }

#if DEBUG
            if (foundHoldings.Count != newFoundHoldings.Count)
            {
                Console.WriteLine($"Merged found holdings. From {foundHoldings.Count} -> {newFoundHoldings.Count}");
            }
#endif

            foundHoldings = newFoundHoldings;
        }

        //Holdings were found. Find the laps and stuff.
#if DEBUG
        DebugPrint(foundHoldings);
#endif

        //TODO: Technically have support for more. But api DTO's does not. So for now, just treat as one. 
        var theHoldingPattern = foundHoldings.MaxBy(x => x.Count) ?? foundHoldings.First();

        // Check that the points in the holding pattern are in a close enough cluster

        foreach (var point in theHoldingPattern)
        {
            foreach (var holding in theHoldingPattern)
            {
                if (point == holding) continue;
                if (!IsCloseEnough(point, holding, ApproximateMaximumRadiusOfHoldingPattern))
                {
                    return new();
                }
            }
        }

        firstInversionPoint = theHoldingPattern.First();
        lastInversionPoint = theHoldingPattern.Last();

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
            Direction = CalculateDirection(cleanedData, firstInversionPoint),
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
            WaypointIsCloseEnough(waypoint, firstInversionPoint, pointBuffer)
        ).ToList();

        var closePoints = waypointsAroundHolding.Where(p =>
            holdingPoints.Any(h => WaypointIsCloseEnough(p, h, 0.025))
        ).ToList();
        if (closePoints.Any())
            return closePoints.First();

        foreach (var point in trafficPositions)
        {
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

    // These methods should be updated with actual distance calculation instead of relying on lat, lon
    private bool WaypointIsCloseEnough(EWayPoint waypoint, TrafficPosition point, double buffer)
    {
        var point1 = new Coord(waypoint.Latitude, waypoint.Longitude);
        var point2 = new Coord(point.Lat, point.Lon);
        return waypoint.Longitude >= point.Lon - buffer && waypoint.Longitude <= point.Lon + buffer &&
               waypoint.Latitude >= point.Lat - buffer && waypoint.Latitude <= point.Lat + buffer;
    }

    private bool IsDistantEnough(TrafficPosition point, TrafficPosition secondPoint, double buffer = 3)
    {
        var point1 = new Coord(point);
        var point2 = new Coord(secondPoint);
        return point1.DistanceTo(point2) >= buffer;
    }

    private bool IsCloseEnough(TrafficPosition point, TrafficPosition secondPoint, double buffer = 12)
    {
        var point1 = new Coord(point);
        var point2 = new Coord(secondPoint);
        var dist = point1.DistanceTo(point2);
        return dist <= buffer;
    }

    private bool IsRecentEnough(TrafficPosition point, TrafficPosition second)
    {
        return second.Clock - point.Clock < 350 && second.Clock - point.Clock > 90;
    }

    private HoldingDirection CalculateDirection(List<TrafficPosition> cleanedData, TrafficPosition firstInversionPoint)
    {
        var secondHolding = cleanedData.SkipWhile(p => p != firstInversionPoint)
            .SkipWhile(p => IsSameDirection(firstInversionPoint.Heading, p.Heading))
            .First();

        return CalculateDirection(firstInversionPoint, secondHolding);
    }

    private HoldingDirection CalculateDirection(TrafficPosition firstInversionPoint, TrafficPosition pointAfterInverionPoint)
    {
        var diff = Math.Abs(firstInversionPoint.Heading - pointAfterInverionPoint.Heading);
        if (diff > 270)
        {
            //Edge case around 0/360
            if (firstInversionPoint.Heading < pointAfterInverionPoint.Heading)
            {
                return HoldingDirection.Left;
            }
            else
            {
                return HoldingDirection.Right;
            }
        }
        else
        {
            var direction = firstInversionPoint.Heading - pointAfterInverionPoint.Heading; // Positive: Left, Negative: Right
            if (direction <= 0)
            {
                return HoldingDirection.Right;
            }
            else
            {
                return HoldingDirection.Left;
            }
        }
    }

    private bool IsSameDirection(double actualHeading, double headingToCheck, double angleBuffer = InvertedHeadingBuffer)
    {
        //Normalize within 360
        actualHeading = actualHeading % 360;
        headingToCheck = headingToCheck % 360;

        if (actualHeading == headingToCheck)
        {
            return true;
        }

        //Check above the buffer
        if (actualHeading + angleBuffer > 360)
        {
            //Edge-case with positive 360
            if (headingToCheck > actualHeading && headingToCheck <= 360)
            {
                return true;
            }
            var remainingAngleToCheck = (actualHeading + angleBuffer) % 360;
            if (headingToCheck <= remainingAngleToCheck && headingToCheck >= 0)
            {
                return true;
            }
        }
        else
        {
            //Normal case check above. No fancy checking around 0 is needed.
            if (headingToCheck >= actualHeading
                && actualHeading + angleBuffer >= headingToCheck)
            {
                return true;
            }
        }

        //Check below the angle
        if (actualHeading - angleBuffer < 0)
        {
            //Edge case with 0
            if (headingToCheck < actualHeading && headingToCheck >= 0)
            {
                return true;
            }
            //Remaing "under" 0 (wraps around to 360)
            var remainingBelow360 = 360 + (actualHeading - angleBuffer);
            if (headingToCheck >= remainingBelow360 && headingToCheck <= 360)
            {
                return true;
            }
        }
        else
        {
            //Normal case check below. No fancy checking around 0 is needed.
            if (headingToCheck < actualHeading && headingToCheck > (actualHeading - angleBuffer))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInvertedHeading(TrafficPosition point, TrafficPosition second)
    {
        return IsSameDirection(point.Heading, second.Heading + 180, InvertedHeadingBuffer);
    }

    private bool IsSameAltitude(TrafficPosition point, TrafficPosition second)
    {
        return point.Alt > second.Alt - AltitudeBuffer
            && point.Alt < second.Alt + AltitudeBuffer;
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
        var lastLat = flightData.Last().Lat;
        var lastLon = flightData.Last().Lon;

        EAirport? dest = null;

        if (_navDbManager is not null)
        {
            dest = _navDbManager.Airports.FirstOrDefault(x => x.ICAO == flightData.Last().Dest);
            dest ??= _navDbManager.Airports.FirstOrDefault(x =>
                                                  x.Longitude >= lastLon + 0.125
                                               && x.Longitude <= lastLon - 0.125
                                               && x.Latitude >= lastLat + 0.125
                                               && x.Latitude <= lastLat - 0.125
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
        return pointToCheck > pointBoundary - CheckDistance
            && pointToCheck < pointBoundary + CheckDistance;
    }

#if DEBUG
    private void DebugPrint(List<List<TrafficPosition>> foundHoldings)
    {
        Console.WriteLine("Copy paste to JavaScript console");
        foreach (var holding in foundHoldings)
        {
            Console.WriteLine("Found a holding pattern: ");
            foreach (var point in holding)
            {
                Console.WriteLine(point.ToJsCommand());
            }
        }
    }
#endif
}