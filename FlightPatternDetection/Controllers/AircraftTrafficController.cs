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
        public AircraftTrafficController(TrafficClient aircraftApi)
        {
            _aircraftApi = aircraftApi ?? throw new ArgumentNullException(nameof(aircraftApi));
        }


        [HttpGet("history/{id}")]
        public async Task<ActionResult<List<TrafficPosition>>> GetAircraftHistoryAsync(long id)
        {
            return Ok((await _aircraftApi.HistoryAsync(id, -1)).ToList());
        }
    }
}
