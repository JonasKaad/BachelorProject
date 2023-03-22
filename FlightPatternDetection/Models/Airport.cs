namespace FlightPatternDetection.Models
{
    public class Airport
    {
        [Key]
        public string ICAO { get; set; }

        public string Name { get; set; }

        public string Country { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

    }
}
