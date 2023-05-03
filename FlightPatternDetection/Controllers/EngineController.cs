

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
        public const double DetectionCheckDistance = 0.5; //Exposing so FlightAnalyzingTask can use it
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
            if (!await _context.Airports.AnyAsync(x => x.ICAO == _ICAO)) // Checks if airport already exists in database
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

            if (request.UseFallback)
            {
                var result = _fallbackController.GetAircraftHistoryAsync(request.FlightId);

                if (result.Result is OkObjectResult okResult && okResult.Value is List<TrafficPosition> positions)
                {
                    // Creates flight
                    if (!await _context.Flights.AnyAsync(x => x.FlightId == request.FlightId))
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
                    }

                    // Origin and Destination, going to be used in creation of Route Information, if they're not null after attempting to fetch airports.
                    Airport origin = null;
                    Airport destination = null;

                    if (GetString(positions, x => x?.Orig ?? string.Empty) != string.Empty)
                    {
                        // Checks first data points and tries to the find closest airport in NavDB
                        EAirport originAirport = AirportNavDB(positions.First().Lat, positions.First().Lon);
                        if (originAirport.Identifier != "_")
                        {
                            var nameICAO = "";
                            if (originAirport.ICAO == "") // If there is no ICAO for the found Airport database in NavDB
                            {
                                nameICAO = originAirport.FullName; // Set the ICAO to be the full name of the Airport
                            }
                            else
                            {
                                nameICAO = originAirport.ICAO; // Otherwise the ICAO is as presented in NavDB
                            }
                            origin = await CreateOrFetchAirport(originAirport.Name, originAirport.Country.Name, nameICAO, originAirport.Latitude, originAirport.Longitude);
                        }
                    }
                    else
                    {
                        // Checks the data and finds the first occurence of the origin Airport ICAO
                        EAirport origAirport = _navDbManager.Airports.First(x => x.ICAO == GetString(positions, x => x?.Orig ?? string.Empty));
                        origin = await CreateOrFetchAirport(origAirport.Name, origAirport.Country.Name, origAirport.ICAO, origAirport.Latitude, origAirport.Longitude);
                    }

                    if (GetString(positions, x => x?.Dest ?? string.Empty) != string.Empty)
                    {
                        // Checks last data points and tries to the find closest airport in NavDB
                        EAirport destinationAirport = AirportNavDB(positions.Last().Lat, positions.Last().Lon);
                        if (destinationAirport.Identifier != "_") // Default case 
                        {
                            var nameICAO = "";
                            if (destinationAirport.ICAO == "") // If there is no ICAO for the found Airport database in NavDB
                            {
                                nameICAO = destinationAirport.FullName; // Set the ICAO to be the full name of the Airport
                            }
                            else
                            {
                                nameICAO = destinationAirport.ICAO; // Otherwise the ICAO is as presented in NavDB
                            }
                            destination = await CreateOrFetchAirport(destinationAirport.Name, destinationAirport.Country.Name, nameICAO, destinationAirport.Latitude, destinationAirport.Longitude);
                        }
                    }
                    else
                    {
                        // Checks the data and finds the first occurence of the destination Airport ICAO
                        EAirport destAirport = _navDbManager.Airports.First(x => x.ICAO == GetString(positions, x => x?.Dest ?? string.Empty));
                        destination = await CreateOrFetchAirport(destAirport.Name, destAirport.Country.Name, destAirport.ICAO, destAirport.Latitude, destAirport.Longitude);
                    }

                    if (origin != null && destination != null)
                    {
                        if (!await _context.RouteInformation.AnyAsync(x => x.FlightId == request.FlightId))
                        {
                            // Creates new entry for the Route Information Table
                            var newRoute = new RouteInformation()
                            {
                                FlightId = request.FlightId,
                                Origin = origin,
                                Destination = destination,
                                Takeoff_Time = DateTimeOffset.FromUnixTimeSeconds(positions.First().Clock).DateTime,
                            };

                            _context.RouteInformation.Add(newRoute);
                        }
                    }
                    await _context.SaveChangesAsync();

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
                // Checks if holdingPattern is already in database
                var FlightID = GetLong(flight, x => x.Id);
                var holdingPattern = _context.HoldingPatterns.FirstOrDefault(x => x.FlightId == FlightID);

                if (holdingPattern == null) // If it is not in DB, add it
                {
                    var newHoldingPattern = new HoldingPattern()
                    {
                        FlightId = GetLong(flight, x => x.Id),
                        Fixpoint = isHolding.FixPoint.Name,
                        Laps = isHolding.Laps,
                        Direction = (Direction)isHolding.Direction,
                        LegDistance = 10, // 10 Nautical Miles
                        Altitude = isHolding.Altitude,
                    };
                    _context.HoldingPatterns.Add(newHoldingPattern);

                    _context.SaveChanges();
                }
            }

            return isHolding;

        }

        private long GetLong(List<TrafficPosition> flight, Func<TrafficPosition?, string> selector)
        {
            long.TryParse(GetString(flight, selector) ?? "-1", out long flightId);
            return flightId;
        }

        /// <summary>
        /// This method looks through the list of TrafficPositions and finds the first case
        /// where the given String is not whitespace or null. I.e. where the entry has some data.
        /// This is used to find occurnces of airports, flightid etc.
        /// </summary>
        /// <param name="flight"></param>
        /// <param name="selector"></param>
        /// <returns>String</returns>
        private string GetString(List<TrafficPosition> flight, Func<TrafficPosition?, string> selector)
        {
            var tempFlight = flight.FirstOrDefault(x => !string.IsNullOrWhiteSpace(selector(x)));
            if (tempFlight != null)
            {
                return selector(tempFlight);
            }
            return "---";
        }
    }
}

