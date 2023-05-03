using Cronos;

namespace FlightPatternDetection.CronJobService
{
    public abstract class CronJobService : IHostedService, IDisposable
    {
        private readonly CronExpression m_expression;
        private readonly TimeZoneInfo m_timeZoneInfo;
        private readonly bool m_runImmediately;

        private System.Timers.Timer m_timer;

        /// <summary>
        /// Will run DoWork() on as a cronjob.
        /// </summary>
        /// <param name="cronExpression">A cron-expression. See https://cron.help/ for help.</param>
        /// <param name="timeZoneInfo"></param>
        /// <param name="serviceOutput">(Optional) Used for logging</param>
        /// <param name="runImmediately">(Optional) If set, the service will run once when it is booted up.</param>
        protected CronJobService(string cronExpression, TimeZoneInfo timeZoneInfo, bool runImmediately = false)
        {
            m_expression = CronExpression.Parse(cronExpression);
            m_timeZoneInfo = timeZoneInfo;
            m_runImmediately = runImmediately;
        }

        protected ILogger? Log { get; set; }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            Log?.LogInformation("CronJobService starting...");
            await ScheduleJob(cancellationToken);
            if (m_runImmediately)
            {
                Log?.LogInformation("CronJobService Running once on boot up..");
                Fire();
            }
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = m_expression.GetNextOccurrence(DateTimeOffset.UtcNow, m_timeZoneInfo);
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.UtcNow;
                if (delay <= TimeSpan.Zero)   // prevent non-positive values from being passed into Timer
                {
                    await ScheduleJob(cancellationToken);
                }

                Log?.LogInformation($"CronJobService will fire next time: {next} (in approx. {delay.Days} day(s), {delay.Hours} hour(s) and {delay.Minutes} minutes)");
                m_timer = new System.Timers.Timer(delay.TotalMilliseconds);
                m_timer.Elapsed += async (sender, args) =>
                {
                    m_timer.Dispose();  // reset and dispose timer
                    m_timer = null;

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await DoWork(cancellationToken);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await ScheduleJob(cancellationToken);    // reschedule next
                    }
                };
                m_timer.Start();
            }
        }

        public abstract Task DoWork(CancellationToken cancellationToken);

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            Log?.LogInformation("CronJobService stopping..");
            m_timer?.Stop();
            return Task.CompletedTask;
        }

        public void Fire()
        {
            if (m_timer is null || !m_timer.Enabled)
            {
                Log?.LogInformation("CronJobService was not fired because the job is already running, or there wasn't a job scheduled. Please wait and try again later.");
                return;
            }
            Log?.LogInformation("CronJobService was manually fired.");
            m_timer.Stop();
            m_timer.Interval = 10;
            m_timer.Start();
        }

        public virtual void Dispose()
        {
            m_timer?.Dispose();
        }
    }
}
