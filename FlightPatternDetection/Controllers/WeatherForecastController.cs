using FlightPatternDetection.DTO;
using FlightPatternDetection.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace FlightPatternDetection.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ApplicationDbContext _context;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("GetAirportExample")]
        public async Task<List<string?>> GetAirports()
        {
            //var allAirports = await _context.Airports.ToListAsync();

            return await _context.Airports.Select(x => x.Name).ToListAsync();
        }

        [HttpPost("CreateAirportExample")]
        public async Task<Airport> CreateAirports(string _Name, string _Country, string _ICAO, double _Latitude, double _Longitude)
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
        public async Task<Flight> GetFlights()
        {
            var testFlight = await _context.Flights.FirstAsync(x => x.FlightId == 1939169898);

            return testFlight;
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