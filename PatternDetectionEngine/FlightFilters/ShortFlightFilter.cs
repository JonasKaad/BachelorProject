using TrafficApiClient;

namespace PatternDetectionEngine.FlightFilters
{
    internal class ShortFlightFilter : IFlightFilter
    {
        private const double AreaCutOff = 0.6d;
        public bool ShouldFilter(List<TrafficPosition> positions)
        {
            double maxLat = double.MinValue, maxLon = double.MinValue;
            double minLat = double.MaxValue, minLon = double.MaxValue;

            foreach (var position in positions)
            {
                maxLat = Math.Max(position.Lat, maxLat);
                maxLon = Math.Max(position.Lon, maxLon);
                minLat = Math.Min(position.Lat, minLat);
                minLon = Math.Min(position.Lon, minLon);
            }

            // Just asuming the earth is flat. It's not, but let's just say it is. 
            var area = Math.Abs(maxLat - minLat) * Math.Abs(maxLon - minLon);
            if (area > AreaCutOff)
            {
                return false;
            }

            return true;
        }
    }
}
