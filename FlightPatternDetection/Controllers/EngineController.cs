

using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Services;
using Microsoft.AspNetCore.Mvc;
using PatternDetectionEngine;
using System.Diagnostics;
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

        private readonly FallbackAircraftTrafficController _fallbackController;

        public EngineController(ILogger<EngineController> logger, TrafficClient trafficClient, NavDbManager navDbManager)
        {
            _logger = logger;
            _trafficClient = trafficClient;
            _navDbManager = navDbManager;

            _fallbackController = new FallbackAircraftTrafficController();

            const double DetectionCheckDistance = 0.5;
            _simpleDetectionEngine = new DetectionEngine(DetectionCheckDistance);
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
                        return Ok(AnalyzeFlightInternal(positions));
                    }
                    else
                    {
                        return NotFound($"{request.FlightId} was not found on ForeFlight servers");
                    }
                }catch(ApiException ex)
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

            return isHolding;
        }
    }
}

