using TrafficApiClient;

namespace PatternDetectionEngine.FlightFilters
{
    internal class AircraftTypeFilter : IFlightFilter
    {
        private readonly List<string> InvalidAircraftTypes = new List<string>()
        {
            "",
            "GRND",
            "GLID",
            "AS30", // Is a type of glider.
        };

        public bool ShouldFilter(List<TrafficPosition> positions)
        {
            if (positions.Any(x => InvalidAircraftTypes.Contains(x.AircraftType)))
            {
                return true;
            }
            return false;
        }
    }
}
