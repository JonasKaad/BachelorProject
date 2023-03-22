using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FlightPatternDetection.Models;

[Table("Holding_Pattern")]
public class HoldingPattern
{
    [Key, ForeignKey(nameof(Flight.Flight_Id))]
    public int Flight_Id { get; set; }

    public string Fixpoint { get; set; }

    public int Laps { get; set; }

    public Direction Direction { get; set; }

    public double Leg_Distance { get; set; }

    public double Altitude { get; set; }


}
