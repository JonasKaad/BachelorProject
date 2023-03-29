using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightPatternDetection.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NavDbController : ControllerBase
    {
        private readonly ILogger<NavDbController> _logger;
        private readonly NavDbManager _navDbManager;

        public NavDbController(ILogger<NavDbController> logger, NavDbManager navDbManager)
        {
            _logger = logger;
            _navDbManager = navDbManager;
        }

        [HttpGet("Waypoints")]
        public IEnumerable<EWayPoint> GetWayPoints(double lat, double lng, double radius = 0.3)
        {
            // How big of radius around the last point, that the waypoints should be fetched for
            var wayPoints = _navDbManager.Waypoints.FindAll(x => ((x.Latitude + radius) >= lat && lat >= (x.Latitude - radius)) && (x.Longitude + radius >= lng && lng >= (x.Longitude - radius))).ToList();
            return wayPoints;

        }
        [HttpGet("Airport")]
        public EAirport GetAirport(string ICAO)
        {

            var airport = _navDbManager.Airports.FirstOrDefault(x => x.ICAO == ICAO);

            if (airport is null)
            {
                return new EAirport(0, 0, "_", 0, 0, $"ICAO: {ICAO} not found");
            }
            return airport;
        }


    }
}
