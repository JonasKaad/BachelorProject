using System.Globalization;
using TrafficApiClient;

namespace PatternDetectionEngine
{
    public static class DebugExtensions
    {
        public static string ToJsCommand(this TrafficPosition point)
        {
            return $"addPoint({point.Lat.ToString(CultureInfo.InvariantCulture)}, {point.Lon.ToString(CultureInfo.InvariantCulture)});";
        }
    }
}
