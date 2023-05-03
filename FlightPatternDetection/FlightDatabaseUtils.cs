using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Models;
using FlightPatternDetection.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using TrafficApiClient;

namespace FlightPatternDetection
{
    public static class FlightDatabaseUtils
    {

        public static async Task RecordInDatabaseAsync(List<TrafficPosition> positions, ApplicationDbContext _dbContext, NavDbManager _navDbManager)
        {
            // Flights

            var FlightId = GetLong(positions, x => x.Id);
            if (!await _dbContext.Flights.AnyAsync(x => x.FlightId == FlightId))
            {
                var newFlight = new Flight()
                {
                    FlightId = GetLong(positions, x => x.Id),
                    Registration = GetString(positions, x => x.Reg),
                    ICAO = GetString(positions, x => x.AircraftType),
                    ModeS = GetString(positions, x => x.Hexid),
                    CallSign = GetString(positions, x => x.Ident),
                };
                _dbContext.Flights.Add(newFlight);
            }

            // Airports

            // Origin and Destination, going to be used in creation of Route Information, if they're not null after attempting to fetch airports.
            Airport origin = null;
            Airport destination = null;

            if (GetString(positions, x => x?.Orig ?? string.Empty) != string.Empty)
            {
                // Checks first data points and tries to the find closest airport in NavDB
                EAirport originAirport = AirportNavDB(positions.First().Lat, positions.First().Lon, _navDbManager);
                if (originAirport.Identifier != "_")
                {
                    var nameICAO = "";
                    if (originAirport.ICAO == "") // If there is no ICAO for the found Airport database in NavDB
                    {
                        nameICAO = originAirport.FullName; // Set the ICAO to be the full name of the Airport
                    }
                    else
                    {
                        nameICAO = originAirport.ICAO; // Otherwise the ICAO is as presented in NavDB
                    }
                    origin = await CreateOrFetchAirport(originAirport.Name, originAirport.Country.Name, nameICAO, originAirport.Latitude, originAirport.Longitude, _dbContext);
                }
            }
            else
            {
                // Checks the data and finds the first occurence of the origin Airport ICAO
                EAirport origAirport = _navDbManager.Airports.First(x => x.ICAO == GetString(positions, x => x?.Orig ?? string.Empty));
                origin = await CreateOrFetchAirport(origAirport.Name, origAirport.Country.Name, origAirport.ICAO, origAirport.Latitude, origAirport.Longitude, _dbContext);
            }

            if (GetString(positions, x => x?.Dest ?? string.Empty) != string.Empty)
            {
                // Checks last data points and tries to the find closest airport in NavDB
                EAirport destinationAirport = AirportNavDB(positions.Last().Lat, positions.Last().Lon, _navDbManager);
                if (destinationAirport.Identifier != "_") // Default case 
                {
                    var nameICAO = "";
                    if (destinationAirport.ICAO == "") // If there is no ICAO for the found Airport database in NavDB
                    {
                        nameICAO = destinationAirport.FullName; // Set the ICAO to be the full name of the Airport
                    }
                    else
                    {
                        nameICAO = destinationAirport.ICAO; // Otherwise the ICAO is as presented in NavDB
                    }
                    destination = await CreateOrFetchAirport(destinationAirport.Name, destinationAirport.Country.Name, nameICAO, destinationAirport.Latitude, destinationAirport.Longitude, _dbContext);
                }
            }
            else
            {
                // Checks the data and finds the first occurence of the destination Airport ICAO
                EAirport destAirport = _navDbManager.Airports.First(x => x.ICAO == GetString(positions, x => x?.Dest ?? string.Empty));
                destination = await CreateOrFetchAirport(destAirport.Name, destAirport.Country.Name, destAirport.ICAO, destAirport.Latitude, destAirport.Longitude, _dbContext);
            }

            // Route Information

            if (origin != null && destination != null)
            {
                if (!await _dbContext.RouteInformation.AnyAsync(x => x.FlightId == FlightId))
                {
                    // Creates new entry for the Route Information Table
                    var newRoute = new RouteInformation()
                    {
                        FlightId = FlightId,
                        Origin = origin,
                        Destination = destination,
                        Takeoff_Time = DateTimeOffset.FromUnixTimeSeconds(positions.First().Clock).DateTime,
                    };

                    _dbContext.RouteInformation.Add(newRoute);
                }
            }
        }

        public static void RecordHoldingPattern(ApplicationDbContext _context, HoldingResult isHolding, List<TrafficPosition> positions)
        {
            var newHoldingPattern = new HoldingPattern()
            {
                FlightId = GetLong(positions, x => x.Id),
                Fixpoint = isHolding.FixPoint.Name,
                Laps = isHolding.Laps,
                Direction = (Direction)isHolding.Direction,
                LegDistance = 10, // 10 Nautical Miles
                Altitude = isHolding.Altitude,
            };
            _context.HoldingPatterns.Add(newHoldingPattern);
        }

        private static EAirport AirportNavDB(double lat, double lon, NavDbManager _navDbManager)
        {
            var airport = _navDbManager.Airports.FirstOrDefault(
                x => x.Latitude <= lat + 0.125 && x.Latitude >= lat - 0.125 &&
                     x.Longitude <= lon + 0.125 && x.Longitude >= lon - 0.125);

            if (airport is null)
            {
                return new EAirport(0, 0, "_", 0, 0, $"Airport at {lat}, {lon} not found");
            }
            return airport;

        }

        private static async Task<Airport> CreateOrFetchAirport(string _Name, object _Country, string _ICAO, double _Lat, double _Lon, ApplicationDbContext _context)
        {
            if (await _context.Airports.FirstOrDefaultAsync(x => x.ICAO == _ICAO) is { } airport) // Checks if airport already exists in database
            {
                return airport;
            }

            var newAirport = new Airport()
            {
                Name = _Name,
                Country = _Country.ToString(),
                ICAO = _ICAO,
                Latitude = _Lat,
                Longitude = _Lon,
            };
            try
            {
                _context.Airports.Add(newAirport);
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException || ex is MySqlException)
                {
                    //It is already there! Let's just assume that it's fine then! :D 
                }
            }
            return newAirport;
        }

        private static long GetLong(List<TrafficPosition> flight, Func<TrafficPosition?, string> selector)
        {
            long.TryParse(GetString(flight, selector) ?? "-1", out long flightId);
            return flightId;
        }

        private static string GetString(List<TrafficPosition> flight, Func<TrafficPosition?, string> selector)
        {
            var tempFlight = flight.FirstOrDefault(x => !string.IsNullOrWhiteSpace(selector(x)));
            if (tempFlight != null)
            {
                return selector(tempFlight);
            }
            return "---";
        }
    }
}