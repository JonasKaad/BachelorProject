using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;

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
        public IEnumerable<EWayPoint> GetWayPoints(double lat, double lng)
        {
            var s = Stopwatch.StartNew();

            var wayPoints = _navDbManager.Waypoints.FindAll(x => ((x.Latitude + 1) >= lat && lat >= (x.Latitude - 1)) && (x.Longitude + 1 >= lng && lng >= (x.Longitude))).ToList();
            s.Stop();
            _logger.LogDebug($"Found {wayPoints.Count} points in {s.Elapsed}");

            return wayPoints;

        }

        [HttpGet("getWayPointCoordinates")]
        public List<List<Double>> GetWayPointCoordinates(double lat, double lng)
        {
            var s = Stopwatch.StartNew();

            var wayPoints = _navDbManager.Waypoints.FindAll(x => ((x.Latitude + 1) >= lat && lat >= (x.Latitude - 1)) && (x.Longitude + 1 >= lng && lng >= (x.Longitude))).ToList();
            s.Stop();
            _logger.LogDebug($"Found {wayPoints.Count} points in {s.Elapsed}");

            var coordinates = new List<List<Double>>();

            foreach (var wayPoint in wayPoints)
            {
                var temp = new List<Double>();
                temp.Add(wayPoint.Latitude);
                temp.Add(wayPoint.Longitude);
                coordinates.Add(temp);
            }

            return coordinates;

        }

    }
}
