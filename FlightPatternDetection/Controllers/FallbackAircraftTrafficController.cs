using FlightPatternDetection.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TrafficApiClient;

namespace FlightPatternDetection.Controllers
{
    [Route("fallback")]
    [ApiController]
    public class FallbackAircraftTrafficController : ControllerBase
    {
        private ApplicationDbContext _context;

        public FallbackAircraftTrafficController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet("history/{id}")]
        public ActionResult<List<TrafficPosition>> GetAircraftHistoryAsync(long id)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "FallbackHistoryData", $"{id}.json");
            if (!System.IO.File.Exists(path))
            {
                //Check if in AutomatedCollection
                if (_context.AutomatedCollection.FirstOrDefault(f => f.FlightId == id) is { } a)
                {
                    var jsonString = a.RawJsonAsString();
                    if (jsonString is null)
                    {
                        return BadRequest($"{nameof(id)} has no json saved");
                    }

                    var flight = JsonConvert.DeserializeObject<List<TrafficPosition>>(jsonString);
                    return Ok(flight);
                }
                return BadRequest($"{nameof(id)} not found in the fallback-db");
            }

            var content = System.IO.File.ReadAllText(path);

            var data = JsonConvert.DeserializeObject<List<TrafficPosition>>(content);
            if (data is null)
            {
                return Problem($"{path} is not a valid JSON file.");
            }

            return Ok(data);
        }

        [HttpGet("list")]
        public ActionResult<List<string>> ListAllFallbackAircrafts()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "FallbackHistoryData");
            var files = System.IO.Directory.GetFiles(path);
            var jsonFiles = files.Where(x => Path.GetExtension(x).ToLower() == ".json")
                                .Select(x => Path.GetFileNameWithoutExtension(x) ?? string.Empty)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList();
            return Ok(jsonFiles);
        }

        [HttpGet("holdings")]
        public ActionResult<List<string>> ListAllHoldings()
        {
            var holdings = _context.AutomatedCollection
                .Where(f => f.DidHold == true)
                .Select(h => h.FlightId.ToString())
                .ToList();

            return Ok(holdings);
        }
    }
}
