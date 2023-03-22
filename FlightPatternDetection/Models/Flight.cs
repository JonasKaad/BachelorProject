using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightPatternDetection.Models;

[Index(nameof(Flight_Id), IsUnique = true)]
public class Flight
{

    [Key]
    public string Flight_Id { get; set; }

    public string Registration { get; set; }

    [Required(ErrorMessage = "ICAO is needed for a flight")]
    public string ICAO { get; set; }

    public string Mode_S { get; set; }

    public string Call_Sign { get; set; }
}
