using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightPatternDetection.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;
        private readonly NavDbManager _navDbManager;

        public DebugController(ILogger<DebugController> logger, NavDbManager navDbManager)
        {
            _logger = logger;
            _navDbManager = navDbManager;
        }

        [HttpGet("waypoints")]
        public IEnumerable<EWayPoint> GetWaypoints(int count = 100)
        {
            return _navDbManager.Waypoints.Take(count);
        }

        [HttpGet("airport")]
        public ActionResult GetAirport(string icao)
        {
            if (_navDbManager.IdentifierToAirport.TryGetValue(icao, out var airport))
            {
                return Ok(airport);
            }
            return NotFound();
        }
    }
}
