using System.Collections.Generic;
using FlightPatternDetection.Models;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;



namespace FlightPatternDetection;

public class ApplicationDbContext : DbContext
{

    public DbSet<Airport> Airports { get; set; }
    public DbSet<Flight> Flights { get; set; }
    public DbSet<Route_Information> Route_Information { get; set; }
    public DbSet<Holding_Pattern> Holding_Patterns { get; set; }



}
