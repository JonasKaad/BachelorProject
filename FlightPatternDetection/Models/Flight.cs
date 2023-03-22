using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightPatternDetection.Models;

[Table("Flight")]
[Index(nameof(FlightId), IsUnique = true)]
public class Flight
{

    [Key]
    [Column(name: "Flight_Id")]
    public string FlightId { get; set; }

    public string Registration { get; set; }

    [Required(ErrorMessage = "ICAO is needed for a flight")]
    public string ICAO { get; set; }

    [Column(name: "Mode_S")]
    public string ModeS { get; set; }


    [Column(name: "Call_Sign")]
    public string CallSign { get; set; }
}
