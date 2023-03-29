using Microsoft.AspNetCore.Mvc;
using TrafficApiClient;
using System.Text.Json;

namespace FlightPatternDetection.Controllers
{
    [Route("traffic")]
    [ApiController]
    public class AircraftTrafficController : ControllerBase
    {
        private readonly TrafficClient _aircraftApi;
        private readonly ILogger<AircraftTrafficController> _logger;
        public AircraftTrafficController(TrafficClient aircraftApi, ILogger<AircraftTrafficController> logger)
        {
            _aircraftApi = aircraftApi ?? throw new ArgumentNullException(nameof(aircraftApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("history/{id}")]
        public async Task<ActionResult<List<TrafficPosition>>> GetAircraftHistoryAsync(long id)
        {
            try
            {
                return Ok((await _aircraftApi.HistoryAsync(id, -1)).ToList());
            }
            catch (ApiException ex)
            {
                _logger.LogError($"ApiException from FF: {ex.Message}\n{ex.StackTrace}");
                return Problem($"Could not reach FF traffic service. Got status {ex.StatusCode}.");
            }
        }
    }
}
