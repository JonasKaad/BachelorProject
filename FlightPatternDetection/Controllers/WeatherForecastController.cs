using FlightPatternDetection.DTO;
using FlightPatternDetection.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<Airport> GetAirports()
        {
            var cphAirport = await _context.Airports.FirstAsync(x => x.ICAO == "EKCH");

            return cphAirport;
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
    }
}