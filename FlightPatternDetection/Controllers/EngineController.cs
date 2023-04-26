

using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Models;
using FlightPatternDetection.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatternDetectionEngine;
using TrafficApiClient;

namespace FlightPatternDetection.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EngineController : ControllerBase
    {
        private readonly ILogger<EngineController> _logger;
        private readonly TrafficClient _trafficClient;
        private readonly NavDbManager _navDbManager;
        private readonly DetectionEngine _simpleDetectionEngine;
        private readonly ApplicationDbContext _context;
        private readonly FallbackAircraftTrafficController _fallbackController;

        public EngineController(ILogger<EngineController> logger, TrafficClient trafficClient, NavDbManager navDbManager, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _trafficClient = trafficClient;
            _navDbManager = navDbManager;
            _context = applicationDbContext;

            _fallbackController = new FallbackAircraftTrafficController();

            const double DetectionCheckDistance = 0.5;
            _simpleDetectionEngine = new DetectionEngine(DetectionCheckDistance, navDbManager);
        }

        private EAirport AirportNavDB(double lat, double lon)
        {
            var airport = _navDbManager.Airports.FirstOrDefault(
                x => x.Latitude <= lat + 0.125 && x.Latitude >= lat - 0.125 &&
                     x.Longitude <= lon + 0.125 && x.Longitude >= lon - 0.125);

            if (airport is null)
            {
                return new EAirport(0, 0, "_", 0, 0, $"Airport at {lat}, {lon} not found");
            }
            return airport;

        }

        private async Task<Airport> CreateOrFetchAirport(string _Name, object _Country, string _ICAO, double _Lat, double _Lon)
        {
            var newAirport = new Airport();
            if (!await _context.Airports.AnyAsync(x => x.ICAO == _ICAO))
            {
                newAirport = new Airport()
                {
                    Name = _Name,
                    Country = _Country.ToString(),
                    ICAO = _ICAO,
                    Latitude = _Lat,
                    Longitude = _Lon,
                };

                _context.Airports.Add(newAirport);
                await _context.SaveChangesAsync();
            }
            else
            {
                newAirport = await _context.Airports.FirstAsync(x => x.ICAO == _ICAO);
            }
            return newAirport;
        }


        [HttpPost("analyze")]
        public async Task<ActionResult<HoldingResult>> AnalyzeFlight(AnalyzeFlightRequest request)
        {
            if (request is null || request.FlightId <= 0)
            {
                return BadRequest("Request must not be null, and the FlightId must be positive");
            }

            //if (await _context.Flights.AnyAsync(x => x.FlightId == request.FlightId))
            //{
            //    if (await _context.HoldingPatterns.AnyAsync(x => x.FlightId == request.FlightId))
            //    {
            //        var HoldingPattern = await _context.HoldingPatterns.FirstAsync(x => x.FlightId == request.FlightId);

            //        var convertedHoldingPattern = new HoldingResult()
            //        {
            //            IsHolding = true,
            //            DetectionTime = TimeSpan.Zero,
            //            Direction = (HoldingDirection)HoldingPattern.Direction,
            //            Altitude = (int)HoldingPattern.Altitude,
            //            Laps = HoldingPattern.Laps,
            //        };
            //        return convertedHoldingPattern;
            //    }
            //    else
            //    {
            //        return new HoldingResult() { IsHolding = false, DetectionTime = TimeSpan.Zero };
            //    }
            //}



            if (request.UseFallback)
            {
                var result = _fallbackController.GetAircraftHistoryAsync(request.FlightId);

                if (result.Result is OkObjectResult okResult && okResult.Value is List<TrafficPosition> positions)
                {

                    var newFlight = new Flight()
                    {
                        FlightId = request.FlightId,
                        Registration = GetString(positions, x => x.Reg),
                        ICAO = GetString(positions, x => x.AircraftType),
                        ModeS = GetString(positions, x => x.Hexid),
                        CallSign = GetString(positions, x => x.Ident),
                    };
                    _context.Flights.Add(newFlight);
                    await _context.SaveChangesAsync();

                    Airport orig = null;
                    Airport dest = null;

                    if (GetString(positions, x => x?.Orig ?? string.Empty) != string.Empty)
                    {
                        EAirport originAirport = AirportNavDB(positions.First().Lat, positions.First().Lon);
                        if (originAirport.Identifier != "_")
                        {
                            var nameICAO = "";
                            if (originAirport.ICAO == "")
                            {
                                nameICAO = originAirport.FullName;
                            }
                            else
                            {
                                nameICAO = originAirport.ICAO;
                            }
                            if (!await _context.Airports.AnyAsync(x => x.ICAO == nameICAO))
                            {
                                orig = await CreateOrFetchAirport(originAirport.Name, originAirport.Country.Name, nameICAO, originAirport.Latitude, originAirport.Longitude);
                            }
                        }
                    }
                    else
                    {
                        EAirport origAirport = _navDbManager.Airports.First(x => x.ICAO == GetString(positions, x => x?.Orig ?? string.Empty));
                        if (!await _context.Airports.AnyAsync(x => x.ICAO == origAirport.ICAO))
                        {
                            orig = await CreateOrFetchAirport(origAirport.Name, origAirport.Country.Name, origAirport.ICAO, origAirport.Latitude, origAirport.Longitude);
                        }
                    }

                    if (GetString(positions, x => x?.Dest ?? string.Empty) != string.Empty)
                    {
                        EAirport destinationAirport = AirportNavDB(positions.Last().Lat, positions.Last().Lon);
                        if (destinationAirport.Identifier != "_")
                        {
                            var nameICAO = "";
                            if (destinationAirport.ICAO == "")
                            {
                                nameICAO = destinationAirport.FullName;
                            }
                            else
                            {
                                nameICAO = destinationAirport.ICAO;
                            }
                            if (!await _context.Airports.AnyAsync(x => x.ICAO == nameICAO))
                            {
                                dest = await CreateOrFetchAirport(destinationAirport.Name, destinationAirport.Country.Name, nameICAO, destinationAirport.Latitude, destinationAirport.Longitude);
                            }

                        }
                    }
                    else
                    {
                        EAirport destAirport = _navDbManager.Airports.First(x => x.ICAO == GetString(positions, x => x?.Dest ?? string.Empty));
                        if (!await _context.Airports.AnyAsync(x => x.ICAO == destAirport.ICAO))
                        {
                            dest = await CreateOrFetchAirport(destAirport.Name, destAirport.Country.Name, destAirport.ICAO, destAirport.Latitude, destAirport.Longitude);
                        }
                    }

                    if (orig != null && dest != null)
                    {
                        if (await _context.RouteInformation.AnyAsync(x => x.FlightId == request.FlightId))
                        {
                            var newRoute = new RouteInformation()
                            {
                                FlightId = request.FlightId,
                                Origin = orig,
                                Destination = dest,
                                Takeoff_Time = DateTimeOffset.FromUnixTimeMilliseconds(positions.First().Clock).DateTime,
                                ATCRoute = "DCT",
                            };

                            _context.RouteInformation.Add(newRoute);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return Ok(AnalyzeFlightInternal(positions));
                }
                else
                {
                    return NotFound($"{request.FlightId} not found in the fallback-db");
                }
            }
            else
            {
                try
                {
                    var positions = (await _trafficClient.HistoryAsync(request.FlightId, -1)).ToList();
                    if (positions.Any())
                    {
                        //
                        return Ok(AnalyzeFlightInternal(positions));
                    }
                    else
                    {
                        return NotFound($"{request.FlightId} was not found on ForeFlight servers");
                    }
                }
                catch (ApiException ex)
                {
                    _logger.LogError($"ApiException from FF: {ex.Message}\n{ex.StackTrace}");
                    return Problem($"Could not reach FF traffic service. Got status {ex.StatusCode}.");
                }
            }
        }

        private HoldingResult AnalyzeFlightInternal(List<TrafficPosition> flight)
        {
            if (flight.Count == 0)
            {
                return new()
                {
                    IsHolding = false,
                    DetectionTime = TimeSpan.Zero,
                };
            }

            var isHolding = _simpleDetectionEngine.AnalyseFlight(flight);

            if (isHolding.IsHolding != false)
            {

                var newHoldingPattern = new HoldingPattern()
                {
                    FlightId = GetLong(flight, x => x.Id), // Takes from the middle element
                    Fixpoint = "xyz",
                    Laps = isHolding.Laps,
                    Direction = (Direction)isHolding.Direction,
                    LegDistance = 12.2,
                    Altitude = isHolding.Altitude,
                };
                _context.HoldingPatterns.Add(newHoldingPattern);

                _context.SaveChanges();
            }
            return isHolding;

        }

        private long GetLong(List<TrafficPosition> flight, Func<TrafficPosition?, string> selector)
        {
            long.TryParse(GetString(flight, selector) ?? "-1", out long flightId);
            return flightId;
        }

        private string GetString(List<TrafficPosition> flight, Func<TrafficPosition?, string> selector)
        {
            return selector(flight.FirstOrDefault(x => !string.IsNullOrWhiteSpace(selector(x))));
        }
    }
}

