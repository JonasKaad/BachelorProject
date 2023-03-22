﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightPatternDetection.Models;


[Table("Airport")]
public class Airport
{
    [Key]
    public string? ICAO { get; set; }

    public string? Name { get; set; }

    public string? Country { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

}
