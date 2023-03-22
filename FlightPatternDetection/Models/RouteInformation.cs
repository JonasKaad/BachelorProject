using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightPatternDetection.Models;

[Table("Route_Information")]
public class RouteInformation
{
    [Key, ForeignKey(nameof(Flight.Flight_Id))]
    [Column(name: "Flight_Id")]
    public int FlightId { get; set; }
    [ForeignKey(nameof(Airport.ICAO))]
    [Column(name: "Destination_ICAO")]
    public string DestinationICAO { get; set; }
    [ForeignKey(nameof(Airport.ICAO))]
    [Column(name: "Origin_ICAO")]
    public string OriginICAO { get; set; }
    public DateTime Takeoff_Time { get; set; }
    [Column(name: "ATC_Route")]
    public string ATCRoute { get; set; }

}
