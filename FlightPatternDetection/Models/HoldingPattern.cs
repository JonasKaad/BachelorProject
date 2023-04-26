using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FlightPatternDetection.Models;

[Table("Holding_Pattern")]
public class HoldingPattern
{
    [Key, ForeignKey("Flight"), Column(name: "Flight_Id")]
    public long FlightId { get; set; }

    public string? Fixpoint { get; set; }

    public int Laps { get; set; }

    [EnumDataType(typeof(int))]
    public Direction Direction { get; set; }

    [Column(name: "Leg_Distance")]
    public double LegDistance { get; set; }

    public double Altitude { get; set; }
}
