namespace FlightPatternDetection.DTO.NavDBEntities
{
    public class EWayPoint : EPointBase
    {
        public EWayPoint(double latitude, double longitude, string identifier, int uid)
            : base(latitude, longitude, identifier, uid)
        { }
    }
}
