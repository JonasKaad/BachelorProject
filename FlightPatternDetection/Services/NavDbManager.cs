using FlightPatternDetection.DTO.NavDBEntities;
using Microsoft.Data.Sqlite;
using System.Data;

namespace FlightPatternDetection.Services
{
    public class NavDbManager
    {
        #region Fields
        private ILogger<NavDbManager> Log { get; }
        private string ConnectionString { get; }

        public List<EAirport> Airports { get; private set; }
        public List<EWayPoint> Waypoints { get; private set; }
        public Dictionary<int, ECountry> Countries { get; private set; }

        public Dictionary<string, EAirport> IdentifierToAirport { get; private set; }
        public Dictionary<int, EAirport> IdToAirport { get; private set; }
        public Dictionary<int, EWayPoint> IdToWaypoints { get; private set; }
        #endregion

        public NavDbManager(IConfiguration configuration, ILogger<NavDbManager> logger)
        {
            ConnectionString = configuration["navDbConnectionString"] ?? throw new ArgumentException("Configuration does not contain any navDbConnectionString. Please add to appsettings.json");
            Log = logger;

            Airports = new List<EAirport>();
            Countries = new Dictionary<int, ECountry>();
            IdentifierToAirport = new Dictionary<string, EAirport>();
            IdToAirport = new Dictionary<int, EAirport>();
            IdToWaypoints = new Dictionary<int, EWayPoint>();
            Waypoints = new List<EWayPoint>();

            Log.LogDebug($"NavDB connectionstring: {ConnectionString}");
            ImportAirportsAndWaypoints();
        }

        protected IDbConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        private void ImportAirportsAndWaypoints()
        {
            InitializeAll();
            Log.LogDebug("Importing airports and waypoints");
            try
            {
                Log.LogDebug("Loading points");
                ImportWaypointsLite();
                ImportVHFPoints();
                ImportNDBPoints();

                Log.LogDebug($"Loaded {Waypoints.Count} Waypoints");

                Log.LogDebug("Loading Countries");
                ImportCountries();

                Log.LogDebug($"Loaded {Countries.Count} Countries");

                Log.LogDebug("Loading Airports");
                ImportAirportLite();

                Log.LogDebug($"Loaded {Airports.Count} Airports");

                Log.LogDebug("Entire NavDB loaded!");
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to import airports {e}", e);
                throw;
            }
        }

        private void ImportAirportLite()
        {
            string query =
                @"SELECT
                        A.Id, A.FullName, A.AirportElevation, A.ICAO, A.City,
                        A.State, A.Country, A.Address,
                        P.FIR, P.Id as UID,
                        P.Identifier, P.Latitude, P.Longitude, P.Id as PointId, P.Name AS Name
                    FROM Airport as A 
                        INNER JOIN Point as P ON A.point = P.id";

            ImportAirportLite(query);
        }

        private void ImportAirportLite(string query)
        {
            try
            {
                using (var connection = GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandTimeout = 300;

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        int latIdx = reader.GetOrdinal("Latitude");
                        int lonIdx = reader.GetOrdinal("Longitude");
                        int uidIdx = reader.GetOrdinal("UID");
                        int identifierIdx = reader.GetOrdinal("Identifier");
                        int airportIdx = reader.GetOrdinal("Id");
                        int pointIdIdx = reader.GetOrdinal("PointId");
                        int pointNameIdx = reader.GetOrdinal("Name");
                        int fullNameIdx = reader.GetOrdinal("FullName");
                        int airportElevationIdx = reader.GetOrdinal("AirportElevation");
                        int icaoIdx = reader.GetOrdinal("ICAO");
                        int cityIdx = reader.GetOrdinal("City");
                        int stateIdx = reader.GetOrdinal("State");
                        int countryIdx = reader.GetOrdinal("Country");
                        int addressIdx = reader.GetOrdinal("Address");

                        while (reader.Read())
                        {
                            var lat = reader.GetDouble(latIdx);
                            var lon = reader.GetDouble(lonIdx);
                            var identifier = reader.GetString(identifierIdx);
                            var name = reader.GetString(pointNameIdx);
                            var fullname = reader.GetString(fullNameIdx);
                            var airportElevation = reader.GetInt32(airportElevationIdx);
                            var uid = reader.GetInt32(uidIdx);
                            var icao = reader[icaoIdx] as string;
                            string? city = reader.GetNullableString(cityIdx);
                            string? state = reader.GetNullableString(stateIdx);
                            int? countryId = reader.GetNullableInt32(countryIdx);
                            string? address = reader.GetNullableString(addressIdx);

                            var airport = new EAirport(lat, lon, identifier, airportElevation, uid, icao);

                            airport.City = city;
                            airport.State = state;
                            airport.Country = null;
                            if (countryId.HasValue && Countries.TryGetValue(countryId.Value, out var country))
                            {
                                airport.Country = country;
                            }

                            airport.Address = address;
                            airport.Name = name;
                            airport.FullName = fullname;
                            Airports.Add(airport);

                            IdentifierToAirport.TryAdd(airport.Identifier, airport);

                            int airportId = reader.GetInt32(airportIdx);
                            int pointId = reader.GetInt32(pointIdIdx);

                            if (IdToAirport == null)
                                IdToAirport = new Dictionary<int, EAirport>();

                            IdToAirport[airportId] = airport;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error loading Airports {e}", e);
                throw;
            }
        }

        private void ImportCountries()
        {
            try
            {
                Countries = new Dictionary<int, ECountry>();

                using (var connection = GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT
                            [Id],
                            [Name],
                            [LocalName],
                            [ISO3166alpha2],
                            [ISO3166alpha3],
                            [ISO3166numeric],
                            [FlagLink]
                        FROM
                            [Country]";
                    command.CommandTimeout = 300;

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        int idIdx = reader.GetOrdinal("Id");
                        int nameIdx = reader.GetOrdinal("Name");
                        int localnameIdx = reader.GetOrdinal("LocalName");
                        int isoalpha2Idx = reader.GetOrdinal("ISO3166alpha2");
                        int isoalpha3Idx = reader.GetOrdinal("ISO3166alpha3");
                        int isoNumericIdx = reader.GetOrdinal("ISO3166numeric");
                        int flagLinkIdx = reader.GetOrdinal("FlagLink");

                        while (reader.Read())
                        {
                            int id = reader.GetInt32(idIdx);
                            string name = reader.GetString(nameIdx);
                            string localName = reader.GetString(localnameIdx);
                            string iso3166alpha2 = reader.GetString(isoalpha2Idx);
                            string iso3166alpha3 = reader.GetString(isoalpha3Idx);
                            int iso3166numeric = reader.GetInt32(isoNumericIdx);
                            string flagLink = reader.GetString(flagLinkIdx);

                            var country = new ECountry()
                            {
                                FlagLink = flagLink,
                                ISO3166alpha2 = iso3166alpha2,
                                ISO3166alpha3 = iso3166alpha3,
                                ISO3166numeric = iso3166numeric,
                                LocalName = localName,
                                Name = name
                            };

                            Countries.Add(id, country);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error loading Countries {e}", e);
                throw;
            }
        }

        private void ImportNDBPoints()
        {
            string query =
                    @"SELECT
                        P.Id, P.Name, P.Identifier, P.Latitude, P.Longitude
                    FROM
                        NDBNavaid WP
                        INNER JOIN Point AS P ON WP.Point = P.Id";

            ImportNDBPoints(query);
        }

        private void ImportNDBPoints(string query)
        {
            try
            {
                using (var connection = GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandTimeout = 300;

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        int latIdx = reader.GetOrdinal("Latitude");
                        int lonIdx = reader.GetOrdinal("Longitude");
                        int identifierIdx = reader.GetOrdinal("Identifier");
                        int pointIdx = reader.GetOrdinal("Id");
                        int nameIdx = reader.GetOrdinal("Name");

                        while (reader.Read())
                        {
                            int pointId = reader.GetInt32(pointIdx);
                            double longitude = reader.GetDouble(lonIdx);
                            double latitude = reader.GetDouble(latIdx);
                            string identifier = reader.GetString(identifierIdx);
                            string name = reader.GetString(nameIdx);

                            var waypoint = new EWayPoint(latitude, longitude, identifier, pointId);

                            waypoint.Name = name;

                            Waypoints.Add(waypoint);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error loading NDB Points {e}", e);
                throw;
            }

        }

        private void ImportVHFPoints()
        {
            string query = @"SELECT
                        P.Id, P.Name, P.Identifier, P.Latitude, P.Longitude
                    FROM
                        VHFNavAid WP
                        INNER JOIN Point AS P ON WP.Point = P.Id";

            ImportVHFPoints(query);
        }

        private void ImportVHFPoints(string query)
        {
            try
            {
                using (var connection = GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandTimeout = 300;

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        int latIdx = reader.GetOrdinal("Latitude");
                        int lonIdx = reader.GetOrdinal("Longitude");
                        int identifierIdx = reader.GetOrdinal("Identifier");
                        int pointIdx = reader.GetOrdinal("Id");
                        int nameIdx = reader.GetOrdinal("Name");

                        while (reader.Read())
                        {
                            int pointId = reader.GetInt32(pointIdx);
                            double longitude = reader.GetDouble(lonIdx);
                            double latitude = reader.GetDouble(latIdx);
                            string identifier = reader.GetString(identifierIdx);
                            string name = reader.GetString(nameIdx);

                            var waypoint = new EWayPoint(latitude, longitude, identifier, pointId)
                            {
                                Name = name,
                            };

                            Waypoints.Add(waypoint);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error loading VHF Points {e}", e);
                throw;
            }
        }

        private void ImportWaypointsLite()
        {
            string query = @"SELECT
                        P.Id, P.Identifier, P.Latitude, P.Longitude, P.ICAO, P.Name
                    FROM
                        Waypoint WP
                        INNER JOIN Point AS P ON WP.Point = P.Id";
            ImportWaypointsLite(query);
        }

        private void ImportWaypointsLite(string query)
        {
            try
            {
                using (var connection = GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandTimeout = 300;
                    SQLitePCL.Batteries.Init();

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        int latIdx = reader.GetOrdinal("Latitude");
                        int lonIdx = reader.GetOrdinal("Longitude");
                        int identifierIdx = reader.GetOrdinal("Identifier");
                        int nameIdx = reader.GetOrdinal("Name");
                        int pointIdx = reader.GetOrdinal("Id");

                        while (reader.Read())
                        {
                            int pointId = reader.GetInt32(pointIdx);

                            double longitude = reader.GetDouble(lonIdx);
                            double latitude = reader.GetDouble(latIdx);
                            string identifier = reader.GetString(identifierIdx);
                            string name = reader.GetString(nameIdx);

                            var waypoint = new EWayPoint(latitude, longitude, identifier, pointId)
                            {
                                Name = name
                            };
                            Waypoints.Add(waypoint);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error loading Waypoints: {e}", e);
                throw;
            }
        }

        private void InitializeAll()
        {
            Airports = new List<EAirport>(30000);
            Countries = new Dictionary<int, ECountry>(300);
            IdentifierToAirport = new Dictionary<string, EAirport>();
            IdToAirport = new Dictionary<int, EAirport>();
            Waypoints = new List<EWayPoint>();
        }
    }
}
