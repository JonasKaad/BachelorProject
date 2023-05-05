using FlightPatternDetection.DTO;
using FlightPatternDetection.DTO.NavDBEntities;
using FlightPatternDetection.Models;
using FlightPatternDetection.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySqlConnector;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Xml.Linq;
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
                    var _ICAO = AirportICAOHandler(originAirport);
                    var _Country = AirportCountryHandler(originAirport);
                    origin = await CreateOrFetchAirport(originAirport.Name, _Country, _ICAO, originAirport.Latitude, originAirport.Longitude, _dbContext);
                }
            }
            else
            {
                // Checks the data and finds the first occurrence of the origin Airport ICAO
                EAirport origAirport = _navDbManager.Airports.First(x => x.ICAO == GetString(positions, x => x?.Orig ?? string.Empty));
                var _ICAO = AirportICAOHandler(origAirport);
                var _Country = AirportCountryHandler(origAirport);
                origin = await CreateOrFetchAirport(origAirport.Name, _Country, _ICAO, origAirport.Latitude, origAirport.Longitude, _dbContext);
            }

            if (GetString(positions, x => x?.Dest ?? string.Empty) != string.Empty)
            {
                // Checks last data points and tries to the find closest airport in NavDB
                EAirport destinationAirport = AirportNavDB(positions.Last().Lat, positions.Last().Lon, _navDbManager);
                if (destinationAirport.Identifier != "_") // Default case 
                {
                    var _ICAO = AirportICAOHandler(destinationAirport);
                    var _Country = AirportCountryHandler(destinationAirport);
                    destination = await CreateOrFetchAirport(destinationAirport.Name, _Country, _ICAO, destinationAirport.Latitude, destinationAirport.Longitude, _dbContext);
                }
            }
            else
            {
                // Checks the data and finds the first occurrence of the destination Airport ICAO
                EAirport destAirport = _navDbManager.Airports.First(x => x.ICAO == GetString(positions, x => x?.Dest ?? string.Empty));
                var _ICAO = AirportICAOHandler(destAirport);
                var _Country = AirportCountryHandler(destAirport);
                destination = await CreateOrFetchAirport(destAirport.Name, _Country, _ICAO, destAirport.Latitude, destAirport.Longitude, _dbContext);
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

        public static void RecordHoldingPattern(ApplicationDbContext dbContext, HoldingResult isHolding, List<TrafficPosition> positions)
        {
            var FlightID = GetLong(positions, x => x.Id);
            var newHoldingPattern = new HoldingPattern()
            {
                FlightId = FlightID,
                Fixpoint = isHolding.FixPoint.Name,
                Laps = isHolding.Laps,
                Direction = isHolding.Direction == HoldingDirection.Right ? Direction.RIGHT : Direction.LEFT,
                LegDistance = 10, // 10 Nautical Miles
                Altitude = isHolding.Altitude,
            };
            dbContext.HoldingPatterns.Add(newHoldingPattern);
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

        private static string AirportICAOHandler(EAirport eAirport)
        {
            string? ICAO = eAirport.ICAO ?? eAirport.FullName;
            return ICAO;
        }

        private static string AirportCountryHandler(EAirport eAirport)
        {
            string? countryName = eAirport.Country?.Name ?? eAirport.City ?? "Country Not Found";
            return countryName;
        }

        private static object _AirportCreationLock = new object();
        private static async Task<Airport> CreateOrFetchAirport(string _Name, string _Country, string _ICAO, double _Lat, double _Lon, ApplicationDbContext _context)
        {
            if (await _context.Airports.FirstOrDefaultAsync(x => x.ICAO == _ICAO) is { } airport) // Checks if airport already exists in database
            {
                return airport;
            }

            lock (_AirportCreationLock)
            {
                if (_context.Airports.FirstOrDefault(x => x.ICAO == _ICAO) is { } LockAirport) // Checks if airport already exists in database
                {
                    return LockAirport;
                }
                var dbConnectionObject = _context.Database.GetDbConnection();
                if (dbConnectionObject.State == System.Data.ConnectionState.Closed)
                {
                    dbConnectionObject.Open();
                }
                using var sqlCommand = dbConnectionObject.CreateCommand();
                sqlCommand.CommandText = "INSERT INTO Airport (Name, Country, ICAO, Latitude, Longitude) VALUES(@Name, @Country, @ICAO, @Lat, @Lon) ON DUPLICATE KEY UPDATE ICAO = ICAO";
                sqlCommand.Parameters.Add(new MySqlParameter("@Name", _Name));
                sqlCommand.Parameters.Add(new MySqlParameter("@Country", _Country));
                sqlCommand.Parameters.Add(new MySqlParameter("@ICAO", _ICAO));
                sqlCommand.Parameters.Add(new MySqlParameter("@Lat", _Lat));
                sqlCommand.Parameters.Add(new MySqlParameter("@Lon", _Lon));
                sqlCommand.ExecuteNonQuery();
                return _context.Airports.First(x => x.ICAO == _ICAO);
            }
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