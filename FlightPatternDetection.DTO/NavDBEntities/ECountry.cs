namespace FlightPatternDetection.DTO.NavDBEntities
{
    public class ECountry
    {
        public string Name { get; set; }
        public string LocalName { get; set; }
        public string ISO3166alpha2 { get; set; }
        public string ISO3166alpha3 { get; set; }
        public int ISO3166numeric { get; set; }
        public string FlagLink { get; set; }
        public string TimeZone { get; set; }
        public int MappingId { get; set; }
    }
}
