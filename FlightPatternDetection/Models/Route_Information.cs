namespace FlightPatternDetection.Models
{
    public class Route_Information
    {
        public int Flight_Id { get; set; }
        public string Destination_ICAO { get; set; }
        public string Origin_ICAO { get; set; }
        public DateTime Takeoff_Time { get; set; }
        public string ATC_Route { get; set; }

    }
}
