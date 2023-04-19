using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Models;
using FlightPatternDetection.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightPatternDetection.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;
        private readonly NavDbManager _navDbManager;
        private readonly ApplicationDbContext _context;

        public DebugController(ILogger<DebugController> logger, NavDbManager navDbManager, ApplicationDbContext context)
        {
            _logger = logger;
            _navDbManager = navDbManager;
            _context = context;
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

        [HttpGet("GetAirportsExample")]
        public async Task<List<string?>> GetAirports()
        {
            //var allAirports = await _context.Airports.ToListAsync();

            return await _context.Airports.Select(x => x.Name).ToListAsync();
        }

        [HttpPost("CreateAirportExample")]
        public async Task<Airport> CreateAirport(string _Name, string _Country, string _ICAO, double _Latitude, double _Longitude)
        {
            var newAirport = new Airport()
            {
                Name = _Name,
                Country = _Country,
                ICAO = _ICAO,
                Latitude = _Latitude,
                Longitude = _Longitude,
            };

            _context.Airports.Add(newAirport);
            await _context.SaveChangesAsync();
            return newAirport;
        }

        [HttpGet("GetFlightExample")]
        public async Task<Flight> GetFlights(string flightid)
        {
            if (await _context.Flights.AnyAsync(x => x.FlightId == int.Parse(flightid)))
            {

                var testFlight = await _context.Flights.FirstAsync(x => x.FlightId == int.Parse(flightid));
                return testFlight;
            }
            else return null;

        }

        [HttpPost("CreateFlightExample")]
        public async Task<Flight> CreateFlight(int flightId, string registration, string iCAO, string modeS, string callSign)
        {
            var newFlight = new Flight()
            {
                FlightId = flightId,
                Registration = registration,
                ICAO = iCAO,
                ModeS = modeS,
                CallSign = callSign,
            };

            _context.Flights.Add(newFlight);
            await _context.SaveChangesAsync();
            return newFlight;
        }

        [HttpGet("GetRouteExample")]
        public async Task<RouteInformation> GetRoutes()
        {
            var testRoute = await _context.RouteInformation.ToListAsync();

            return testRoute.First();
        }

        [HttpGet("GetHoldingPatternExample")]
        public async Task<HoldingPattern> GetHoldingPattern()
        {
            var testPattern = await _context.HoldingPatterns.ToListAsync();

            return testPattern.First();
        }

        [HttpPost("CreateHoldingPatternExample")]
        public async Task<HoldingPattern> CreateHoldingPattern(int flightId, string fixpoint, int laps, Direction direction, double legDistance, double altitude)
        {
            var newHoldingPattern = new HoldingPattern()
            {
                FlightId = flightId,
                Fixpoint = fixpoint,
                Laps = laps,
                Direction = direction,
                LegDistance = legDistance,
                Altitude = altitude,
            };
            _context.HoldingPatterns.Add(newHoldingPattern);
            await _context.SaveChangesAsync();
            return newHoldingPattern;
        }
    }
}
