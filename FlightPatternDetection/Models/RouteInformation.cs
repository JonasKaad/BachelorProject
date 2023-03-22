using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightPatternDetection.Models;

[Table("Route_Information")]
public class RouteInformation
{
    [Key, ForeignKey("Flight"), Column(name: "Flight_Id")]
    public int FlightId { get; set; }

    [ForeignKey("Destination_ICAO")]
    public Airport Destination { get; set; }

    [ForeignKey("Origin_ICAO")]
    public Airport Origin { get; set; }
    public DateTime Takeoff_Time { get; set; }
    [Column(name: "ATC_Route")]
    public string ATCRoute { get; set; }

}
