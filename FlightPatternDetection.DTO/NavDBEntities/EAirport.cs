namespace FlightPatternDetection.DTO.NavDBEntities
{
    public class EAirport : EPointBase
    {
        public EAirport(double latitude, double longitude, string identifier, int airportElevation, int uid, string icao)
            : base(latitude, longitude, identifier, uid)
        {
            Identifier = identifier;
            ICAO = icao;
            Name = identifier;
            Elevation = airportElevation;
        }

        public string ICAO { get; set; }
        public string FullName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public ECountry Country { get; set; }
        public string Address { get; set; }
        public double Elevation { get; }
    }
}
