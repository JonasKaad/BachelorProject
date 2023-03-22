namespace FlightPatternDetection.Models
{
    public class Holding_Pattern
    {
        [Key, ForeignKey(nameof(Flight.Flight_Id))]
        public int Flight_Id { get; set; }

        public string Fixpoint { get; set; }

        public int Laps { get; set; }

        public enum Direction
        {
            Right,
            Left
        }

        public double Leg_Distance { get; set; }

        public double Altitude { get; set; }


    }
}
