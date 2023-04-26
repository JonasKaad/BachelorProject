using TrafficApiClient;

namespace PatternDetectionEngine.FlightFilters
{
    internal interface IFlightFilter
    {
        bool ShouldFilter(List<TrafficPosition> positions);
    }
}
