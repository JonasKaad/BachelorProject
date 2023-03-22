using Newtonsoft.Json;
using TrafficApiClient;

namespace UnitTests;

public static class UnitTestHelper
{
    public static List<TrafficPosition> RetriveFlightData(string json)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "resources", json);
        if (!System.IO.File.Exists(path))
        {
            throw new ArgumentException("File not found");
        }

        var content = System.IO.File.ReadAllText(path);

        var data = JsonConvert.DeserializeObject<List<TrafficPosition>>(content);
        if (data is null)
        {
            throw new Exception("Not valid JSON file");
        }

        return data;
    }
}