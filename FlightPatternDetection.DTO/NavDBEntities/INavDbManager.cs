using FlightPatternDetection.DTO.NavDBEntities;

namespace FlightPatternDetection.Services;

public interface INavDbManager
{
    List<EAirport> Airports { get; }
    List<EWayPoint> Waypoints { get; }
    Dictionary<int, ECountry> Countries { get; }
    Dictionary<string, EAirport> IdentifierToAirport { get; }
    Dictionary<int, EAirport> IdToAirport { get; }
    Dictionary<int, EWayPoint> IdToWaypoints { get; }
}