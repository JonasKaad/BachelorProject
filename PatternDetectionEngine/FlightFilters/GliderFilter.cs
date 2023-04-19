using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrafficApiClient;

namespace PatternDetectionEngine.FlightFilters
{
    internal class GliderFilter : IFlightFilter
    {
        private readonly List<string> GliderAircraftTypes = new List<string>()
        {
            "GLID",
            "AS30"
        };

        public bool ShouldFilter(List<TrafficPosition> positions)
        {
            if (positions.Any(x => GliderAircraftTypes.Contains(x.AircraftType)))
            {
                return true;
            }
            return false;
        }
    }
}
