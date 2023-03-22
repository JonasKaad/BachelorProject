using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightPatternDetection.Models;

public class Route_Information
{
    [Key, ForeignKey(nameof(Flight.Flight_Id))]
    public int Flight_Id { get; set; }
    [ForeignKey(nameof(Airport.ICAO))]
    public string Destination_ICAO { get; set; }
    [ForeignKey(nameof(Airport.ICAO))]
    public string Origin_ICAO { get; set; }
    public DateTime Takeoff_Time { get; set; }
    public string ATC_Route { get; set; }

}
