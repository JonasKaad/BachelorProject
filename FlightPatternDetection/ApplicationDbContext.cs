using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using FlightPatternDetection.Models;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;



namespace FlightPatternDetection;

public class ApplicationDbContext : DbContext
{
    public DbSet<Airport> Airports { get; set; }
    public DbSet<Flight> Flights { get; set; }
    public DbSet<RouteInformation> Route_Information { get; set; }
    public DbSet<HoldingPattern> Holding_Patterns { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> context) : base(context)
    {
        ;
    }

}
