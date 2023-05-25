using FlightPatternDetection.CronJobService;
using FlightPatternDetection.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TrafficApiClient;
using TrafficStreamingApiClient;

namespace FlightPatternDetection
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddRazorPages();
            builder.Services.AddLogging((config) =>
            {
                config.AddConsole();
            });

            builder.Services.AddSingleton(sp =>
            {
                var client = new TrafficClient(builder.Configuration["trafficClientEndpoint"]);
                client.SetApiKey(builder.Configuration["trafficApiKey"]);
                return client;
            });
            builder.Services.AddSingleton(sp => new TrafficStreamingClient(builder.Configuration["trafficStreamingClientEndpoint"]));
            builder.Services.AddSingleton<NavDbManager>();
            builder.Services.AddSwaggerGen();

            if (bool.TryParse(builder.Configuration["enableAutomatedCollection"], out bool enableAutomatedCollection) && enableAutomatedCollection)
            {
                builder.Services.AddCronJob<FlightAccumulationTask>(c =>
                {
                    c.TimeZoneInfo = TimeZoneInfo.Utc;
                    c.RunImmediately = true;
                    c.CronExpression = "55 * * * *"; //Every :55 on the clock
                });

                builder.Services.AddCronJob<FlightAnalyzingTask>(c =>
                {
                    c.TimeZoneInfo = TimeZoneInfo.Utc;
                    c.RunImmediately = true;
                    c.CronExpression = "*/30 * * * *"; //Every 30 minutes
                });
            }

            // Replace with your connection string.
            var connectionString = builder.Configuration["mysqlConnectionString"];

            // Replace with your server version and type.
            // Use 'MariaDbServerVersion' for MariaDB.
            // Alternatively, use 'ServerVersion.AutoDetect(connectionString)'.
            // For common usages, see pull request #1233.
            var serverVersion = new MariaDbServerVersion(new Version(8, 0, 31));

            // Replace 'YourDbContext' with the name of your own DbContext derived class.
            builder.Services.AddDbContext<ApplicationDbContext>(
                dbContextOptions => dbContextOptions
                    .UseMySql(connectionString, serverVersion)
                    // The following three options help with debugging, but should
                    // be changed or removed for production.
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
            );


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}