using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightPatternDetection.DTO
{
    public class FlightData
    {
        public string AircraftICAO { get; set; } //Called aircraftType in the JSON
        public string AircraftId { get; set; }
        public string AircraftRegistration { get; set; }
        public string AirportOrigin { get; set; }
        public string AirportDestination { get; set; }


    }
}
