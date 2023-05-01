using System;

namespace FlightPatternDetection.CronJobService
{
    public interface IScheduleConfig<T>
    {
        string CronExpression { get; set; }
        TimeZoneInfo TimeZoneInfo { get; set; }
        bool RunImmediately { get; set; }
    }

    public class ScheduleConfig<T> : IScheduleConfig<T>
    {
        public string CronExpression { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }
        public bool RunImmediately { get; set; }
    }
}
