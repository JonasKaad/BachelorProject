

using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Models;
using FlightPatternDetection.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatternDetectionEngine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Xml.Linq;
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


        [HttpPost("analyze")]
        public async Task<ActionResult<HoldingResult>> AnalyzeFlight(AnalyzeFlightRequest request)
        {
            if (request is null || request.FlightId <= 0)
            {
                return BadRequest("Request must not be null, and the FlightId must be positive");
            }

            if (await _context.Flights.AnyAsync(x => x.FlightId == request.FlightId))
            {
                if (await _context.HoldingPatterns.AnyAsync(x => x.FlightId == request.FlightId))
                {
                    var HoldingPattern = await _context.HoldingPatterns.FirstAsync(x => x.FlightId == request.FlightId);

                    var convertedHoldingPattern = new HoldingResult()
                    {
                        IsHolding = true,
                        DetectionTime = TimeSpan.Zero,
                        Direction = (HoldingDirection)HoldingPattern.Direction,
                        Altitude = (int)HoldingPattern.Altitude,
                        Laps = HoldingPattern.Laps,
                    };
                    return convertedHoldingPattern;
                }
                else
                {
                    return new HoldingResult() { IsHolding = false, DetectionTime = TimeSpan.Zero };
                }
            }
            else
            {

                if (request.UseFallback)
                {
                    var result = _fallbackController.GetAircraftHistoryAsync(request.FlightId);

                    if (result.Result is OkObjectResult okResult && okResult.Value is List<TrafficPosition> positions)
                    {
                        //var MiddleValue = (int)Math.Floor(positions.Count / 2.0);// Takes middle element
                        var MiddleValueOfData = positions.Skip((int)Math.Floor(positions.Count / 2.0)).First();// Takes middle element

                        var newFlight = new Flight()
                        {
                            FlightId = (int)request.FlightId,
                            Registration = MiddleValueOfData.Reg,
                            ICAO = MiddleValueOfData.AircraftType,
                            ModeS = MiddleValueOfData.Hexid,
                            CallSign = MiddleValueOfData.Ident,
                        };
                        _context.Flights.Add(newFlight);
                        await _context.SaveChangesAsync();



                        if (MiddleValueOfData.Orig == "")
                        {
                            EAirport newAirport = AirportNavDB(MiddleValueOfData.Lat, MiddleValueOfData.Lon);

                        }
                        if (!await _context.Airports.AnyAsync(x => x.ICAO == MiddleValueOfData.Airport))
                        {
                            //var newAirport = new Airport()
                            //{
                            //    Name = _navDbManager.IdentifierToAirport,
                            //    Country = _Country,
                            //    ICAO = _ICAO,
                            //    Latitude = _Latitude,
                            //    Longitude = _Longitude,
                            //};

                            //_context.Airports.Add(newAirport);
                            //await _context.SaveChangesAsync();
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
                    FlightId = int.Parse(flight.Skip((int)Math.Floor(flight.Count / 2.0)).First().Id), // Takes from the middle element
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
    }
}

