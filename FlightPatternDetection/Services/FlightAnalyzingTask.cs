﻿using FlightPatternDetection.Controllers;
using FlightPatternDetection.CronJobService;
using FlightPatternDetection.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Newtonsoft.Json;
using PatternDetectionEngine;
using TrafficApiClient;

namespace FlightPatternDetection.Services
{
    public class FlightAnalyzingTask : CronJobService.CronJobService
    {
        private IServiceScope m_applicationScope;
        private ApplicationDbContext m_dbContext;
        private DetectionEngine m_engine;
        private NavDbManager m_navDbManager;
        private TrafficClient m_trafficClient;
        public FlightAnalyzingTask(ILogger<FlightAnalyzingTask> logger, TrafficClient trafficClient, IServiceProvider services, NavDbManager navDbManager, IScheduleConfig<FlightAnalyzingTask> config)
        : base(config.CronExpression, config.TimeZoneInfo, config.RunImmediately)
        {
            Log = logger;
            m_applicationScope = services.CreateScope();
            var serviceProvider = m_applicationScope.ServiceProvider ?? throw new ApplicationException("Unable to fetch service-provider in " + nameof(FlightAnalyzingTask));
            m_dbContext = serviceProvider.GetService<ApplicationDbContext>() ?? throw new ApplicationException("Service-provider contains no " + nameof(ApplicationDbContext));

            m_engine = new DetectionEngine(EngineController.DetectionCheckDistance, navDbManager);
            m_trafficClient = trafficClient;
            m_navDbManager = navDbManager;
        }

        public async override Task DoWork(CancellationToken cancellationToken)
        {
            await Task.Delay(5000);
            Log?.LogInformation("Started flight analyzing");

            var fetchFlightsBefore = DateTime.UtcNow.Subtract(TimeSpan.FromHours(TrafficApiConstants.HoursToWaitBeforeAnalyzingFlight));
            var earliestTimeDataIsAvalaiable = DateTime.UtcNow.Subtract(TimeSpan.FromDays(TrafficApiConstants.DaysDataIsKept));
            var flightsToAnalyze = new List<AutomatedCollection>();
            try
            {
                flightsToAnalyze = await m_dbContext.AutomatedCollection.Where(x => !x.IsProcessed
                                                                                && x.Fetched > earliestTimeDataIsAvalaiable
                                                                                && x.Fetched < fetchFlightsBefore)
                                                                            .OrderBy(x => x.Fetched)
                                                                            .Take(TrafficApiConstants.MaxFlightsToAnalyzeAtOneTime)
                                                                            .ToListAsync();
            }
            catch (MySqlException ex)
            {
                Log?.LogError($"Failed to fetch flights to analyze.. Something might be wrong.. {ex.Message}");
                return;
            }

            Log?.LogInformation($"Fetched {flightsToAnalyze.Count} flights to analyze.");

            const int BatchSize = 25;
            int currentBatch = 0;
            int failedAttempts = 0;
            int totalProcessed = 0;
            if (flightsToAnalyze.Any())
            {
                foreach (var flight in flightsToAnalyze)
                {
                    if (failedAttempts > 10)
                    {
                        Log?.LogError($"We had {failedAttempts} trying to fetch flights.. Skipping the rest and trying again later..");
                        break;
                    }

                    flight.IsProcessed = true;
                    var airportsAdded = new List<string>();
                    List<TrafficPosition>? flightData = null;
                    try
                    {
                        flightData = (await m_trafficClient.HistoryAsync(flight.FlightId, maxAgeSeconds: null)).ToList();

                        if (!flightData.Any())
                        {
                            continue;
                        }

                        var result = m_engine.AnalyseFlight(flightData);
                        await FlightDatabaseUtils.RecordInDatabaseAsync(flightData, m_dbContext, m_navDbManager);
                        flight.DidHold = result.IsHolding;
                        if (result.IsHolding)
                        {
                            FlightDatabaseUtils.RecordHoldingPattern(m_dbContext, result, flightData);
                            flight.RawJson = ZipUtils.ZipData(JsonConvert.SerializeObject(flightData));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is ApiException apiEx)
                        {
                            failedAttempts++;
                            Log?.LogWarning($"Failed to fetch history for a single flight. FF-Api returned: {apiEx.StatusCode}");
                        }
                        else if (ex is MySqlException)
                        {
                            ; // Don't care, not as important, as it's only the airports that messes things up here.
                        }
                        else
                        {
                            //Other unhandled exception                           
                            Log?.LogError($"Failed due to other unhandled exceptions: {ex}");
                            flight.DidHold = null;
                            if (flightData is not null)
                            {
                                flight.RawJson = ZipUtils.ZipData(JsonConvert.SerializeObject(flightData));
                            }
                            else
                            {
                                flight.RawJson = ZipUtils.ZipData(JsonConvert.SerializeObject(ex));
                            }
                        }
                    }

                    if (++currentBatch >= BatchSize)
                    {
                        try
                        {
                            totalProcessed += await m_dbContext.SaveChangesAsync();
                            currentBatch = 0;
                        }
                        catch (Exception ex)
                        {
                            Log?.LogError($"Failed to update analyzed flights in db.. Something might be wrong.. {ex.Message}");
                            break;
                        }
                    }
                }

                //Final save..
                try
                {
                    totalProcessed += await m_dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Log?.LogError($"Failed to update analyzed flights in db.. Something might be wrong.. {ex.Message}");
                }
                Log?.LogInformation($"Processed and analyzed {totalProcessed} flights..");
            }
        }

        public override void Dispose()
        {
            m_applicationScope.Dispose();
            base.Dispose();
        }
    }
}
