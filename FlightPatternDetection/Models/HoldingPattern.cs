using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FlightPatternDetection.Models;

[Table("Holding_Pattern")]
public class HoldingPattern
{
    [Key, ForeignKey(nameof(Flight.FlightId))]
    [Column(name: "Flight_Id")]
    public int FlightId { get; set; }

    public string Fixpoint { get; set; }

    public int Laps { get; set; }

    public Direction Direction { get; set; }

    [Column(name: "Leg_Distance")]
    public double LegDistance { get; set; }

    public double Altitude { get; set; }


}
