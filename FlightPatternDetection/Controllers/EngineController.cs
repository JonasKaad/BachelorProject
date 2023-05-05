

using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Models;
using FlightPatternDetection.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
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

        [HttpPost("analyze")]
        public async Task<ActionResult<HoldingResult>> AnalyzeFlight(AnalyzeFlightRequest request)
        {
            int failedAttempts = 0;
            if (request is null || request.FlightId <= 0)
            {
                return BadRequest("Request must not be null, and the FlightId must be positive");
            }

            if (request.UseFallback)
            {
                var result = _fallbackController.GetAircraftHistoryAsync(request.FlightId);

                if (result.Result is OkObjectResult okResult && okResult.Value is List<TrafficPosition> positions)
                {
                    await FlightDatabaseUtils.RecordInDatabaseAsync(positions, _context, _navDbManager);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (MySqlException ex)
                    {
                        // oh no
                        failedAttempts++;
                        _logger?.LogWarning($"Failed to fetch history for a single." + " Total fails: " + failedAttempts);
                        //{ ex.Message};
                    }
                    return Ok(await AnalyzeFlightInternalAsync(positions));
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
                        return Ok(await AnalyzeFlightInternalAsync(positions));
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

        private async Task<HoldingResult> AnalyzeFlightInternalAsync(List<TrafficPosition> flight)
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
                var holdingPattern = await _context.HoldingPatterns.FirstOrDefaultAsync(x => x.FlightId == FlightID);

                if (holdingPattern == null) // If it is not in DB, add it
                {
                    FlightDatabaseUtils.RecordHoldingPattern(_context, isHolding, flight);
                    await _context.SaveChangesAsync();
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

