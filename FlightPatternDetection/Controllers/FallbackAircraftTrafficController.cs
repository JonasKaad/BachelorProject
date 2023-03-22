using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TrafficApiClient;

namespace FlightPatternDetection.Controllers
{
    [Route("fallback")]
    [ApiController]
    public class FallbackAircraftTrafficController : ControllerBase
    {
        [HttpGet("history/{id}")]
        public ActionResult<List<TrafficPosition>> GetAircraftHistoryAsync(long id)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "FallbackHistoryData", $"{id}.json");
            if (!System.IO.File.Exists(path))
            {
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
    }
}
