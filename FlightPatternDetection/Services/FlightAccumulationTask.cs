using FlightPatternDetection.CronJobService;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using TrafficStreamingApiClient;

namespace FlightPatternDetection.Services
{
    public class FlightAccumulationTask : CronJobService.CronJobService
    {
        private IServiceScope m_applicationScope;
        private ApplicationDbContext m_dbContext;
        private TrafficStreamingClient m_trafficStreamingClient;
        public FlightAccumulationTask(ILogger<FlightAccumulationTask> logger, TrafficStreamingClient trafficStreamingClient, IServiceProvider services, IScheduleConfig<FlightAccumulationTask> config)
        : base(config.CronExpression, config.TimeZoneInfo, config.RunImmediately)
        {
            Log = logger;
            m_applicationScope = services.CreateScope();
            var serviceProvider = m_applicationScope.ServiceProvider ?? throw new ApplicationException("Unable to fetch service-provider in " + nameof(FlightAccumulationTask));
            m_dbContext = serviceProvider.GetService<ApplicationDbContext>() ?? throw new ApplicationException("Service-provider contains no " + nameof(ApplicationDbContext));
            m_trafficStreamingClient = trafficStreamingClient;
        }

        public async override Task DoWork(CancellationToken cancellationToken)
        {
            Log?.LogInformation("Fetching flight IDs");
            List<string> flightIdStr;
            try
            {
                flightIdStr = (await m_trafficStreamingClient.AllAsync(amount: null, cancellationToken)).Select(x => x.Id).ToList();
            }
            catch (ApiException ex)
            {
                Log?.LogError("Failed to fetch flight ids!: ", ex.Message);
                return;
            }

            if (flightIdStr is null || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (flightIdStr.Count == 0)
            {
                Log?.LogWarning("There were no flight ids to fetch! Something might be wrong..");
                return;
            }

            var earliestTimeDataIsAvalaiable = DateTime.UtcNow.Subtract(TimeSpan.FromDays(TrafficApiConstants.DaysDataIsKept));

            var allCurrentIds = await m_dbContext.AutomatedCollection
                .Where(x => x.Fetched > earliestTimeDataIsAvalaiable) //Makes sure we limit how many Ids we're fetching so we never overload things.
                .Select(x => x.FlightId).ToListAsync();

            foreach (var stringId in flightIdStr)
            {
                if (long.TryParse(stringId, out long id))
                {
                    if (allCurrentIds.Contains(id))
                    {
                        continue;
                    }
                    await m_dbContext.AutomatedCollection.AddAsync(new()
                    {
                        FlightId = id,
                        Fetched = DateTime.UtcNow,
                        IsProcessed = false
                    });
                }
            }

            try
            {
                var affectedRows = await m_dbContext.SaveChangesAsync();
                Log?.LogInformation($"Added {affectedRows} to the database for data mining.");
            }
            catch (MySqlException e)
            {
                Log?.LogError($"MySql encountered aan error.. Try again later lol: {e.Message}");
            }
        }

        public override void Dispose()
        {
            m_applicationScope.Dispose();
            base.Dispose();
        }
    }
}
