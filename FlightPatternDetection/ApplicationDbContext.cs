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
    public DbSet<RouteInformation> RouteInformation { get; set; }
    public DbSet<HoldingPattern> HoldingPatterns { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> context) : base(context)
    {
        ;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<RouteInformation>().Navigation(e => e.Destination).AutoInclude();
        modelBuilder.Entity<RouteInformation>().Navigation(e => e.Origin).AutoInclude();
        //modelBuilder.Entity<HoldingPattern>().Property(p => p.Direction).HasColumnType("Direction");
        modelBuilder.Entity<HoldingPattern>().Property(e => e.Direction)
           .HasMaxLength(50)
           .HasConversion(
               v => v.ToString(),
               v => (Direction)Enum.Parse(typeof(Direction), v))
               .IsUnicode(false);
    }
}
