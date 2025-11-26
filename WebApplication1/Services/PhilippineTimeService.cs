namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Centralized service for Philippines Standard Time (PHT, UTC+8) operations.
    /// CRITICAL: All datetime operations in this application MUST use this service
    /// to ensure consistent timezone handling regardless of server location.
    /// 
    /// Why this matters:
    /// - Server may run on UTC, US time, or any other timezone
    /// - Attendance logic (late detection, absence marking) must use PHT
    /// - Database stores timestamps that need consistent interpretation
    /// - Frontend displays must show PHT, not user's local time
    /// </summary>
    public class PhilippineTimeService
    {
        // Philippines uses UTC+8, same as Singapore Standard Time
        // "Singapore Standard Time" works on Windows; "Asia/Manila" works on Linux
        private static readonly string WindowsTimeZoneId = "Singapore Standard Time";
        private static readonly string IanaTimeZoneId = "Asia/Manila";
        
        private readonly TimeZoneInfo _philippineTimeZone;
        private readonly ILogger<PhilippineTimeService> _logger;

        public PhilippineTimeService(ILogger<PhilippineTimeService> logger)
        {
            _logger = logger;
            _philippineTimeZone = GetPhilippineTimeZone();
            _logger.LogInformation($"PhilippineTimeService initialized with timezone: {_philippineTimeZone.DisplayName}");
        }

        /// <summary>
        /// Get the Philippine TimeZoneInfo (cross-platform compatible)
        /// </summary>
        private static TimeZoneInfo GetPhilippineTimeZone()
        {
            try
            {
                // Try Windows timezone ID first
                return TimeZoneInfo.FindSystemTimeZoneById(WindowsTimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    // Fall back to IANA timezone ID (Linux/macOS)
                    return TimeZoneInfo.FindSystemTimeZoneById(IanaTimeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Last resort: create a custom timezone for UTC+8
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "PHT",
                        TimeSpan.FromHours(8),
                        "Philippine Standard Time",
                        "Philippine Standard Time");
                }
            }
        }

        /// <summary>
        /// Get current Philippine time (equivalent to DateTime.Now in Philippines)
        /// USE THIS instead of DateTime.Now throughout the application
        /// </summary>
        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _philippineTimeZone);

        /// <summary>
        /// Get current Philippine date (equivalent to DateTime.Today in Philippines)
        /// USE THIS instead of DateTime.Today throughout the application
        /// </summary>
        public DateTime Today => Now.Date;

        /// <summary>
        /// Get current Philippine time of day
        /// </summary>
        public TimeSpan TimeOfDay => Now.TimeOfDay;

        /// <summary>
        /// Get current day of week in Philippine time
        /// </summary>
        public string DayOfWeek => Now.ToString("dddd");

        /// <summary>
        /// Convert UTC datetime to Philippine time
        /// </summary>
        public DateTime FromUtc(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Local)
            {
                _logger.LogWarning("FromUtc called with Local datetime. Converting to UTC first.");
                utcDateTime = utcDateTime.ToUniversalTime();
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _philippineTimeZone);
        }

        /// <summary>
        /// Convert Philippine time to UTC
        /// </summary>
        public DateTime ToUtc(DateTime philippineDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(philippineDateTime, DateTimeKind.Unspecified),
                _philippineTimeZone);
        }

        /// <summary>
        /// Parse a date string and return it as Philippine time
        /// </summary>
        public DateTime ParseAsPhilippineTime(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime parsed))
            {
                return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Unspecified);
            }
            return Today;
        }

        /// <summary>
        /// Format datetime for display (ensures consistent PHT display)
        /// </summary>
        public string FormatForDisplay(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return dateTime.ToString(format);
        }

        /// <summary>
        /// Format date for HTML date input (yyyy-MM-dd)
        /// </summary>
        public string FormatForDateInput(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Format time for display (hh:mm:ss tt)
        /// </summary>
        public string FormatTimeForDisplay(DateTime dateTime)
        {
            return dateTime.ToString("hh:mm:ss tt");
        }

        /// <summary>
        /// Format time for display (hh:mm tt)
        /// </summary>
        public string FormatTimeShort(TimeSpan time)
        {
            return DateTime.Today.Add(time).ToString("hh:mm tt");
        }

        /// <summary>
        /// Check if a given time is within class schedule (for attendance validation)
        /// All comparisons done in PHT
        /// </summary>
        public bool IsWithinSchedule(TimeSpan currentTime, TimeSpan scheduleStart, TimeSpan scheduleEnd)
        {
            return currentTime >= scheduleStart && currentTime <= scheduleEnd;
        }

        /// <summary>
        /// Determine if student is late based on grace period
        /// </summary>
        /// <param name="scanTime">Time of scan (PHT)</param>
        /// <param name="classStartTime">Class start time</param>
        /// <param name="gracePeriodMinutes">Grace period in minutes</param>
        public bool IsLate(TimeSpan scanTime, TimeSpan classStartTime, int gracePeriodMinutes)
        {
            var graceEnd = classStartTime.Add(TimeSpan.FromMinutes(gracePeriodMinutes));
            return scanTime > graceEnd;
        }

        /// <summary>
        /// Get the TimeZoneInfo for injection where needed
        /// </summary>
        public TimeZoneInfo TimeZone => _philippineTimeZone;
    }

    /// <summary>
    /// Static helper for cases where DI is not available (e.g., Razor views)
    /// </summary>
    public static class PhilippineTime
    {
        private static readonly TimeZoneInfo _timeZone;

        static PhilippineTime()
        {
            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }
            catch
            {
                try
                {
                    _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
                }
                catch
                {
                    _timeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "PHT", TimeSpan.FromHours(8), "Philippine Standard Time", "Philippine Standard Time");
                }
            }
        }

        /// <summary>
        /// Get current Philippine time
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

        /// <summary>
        /// Get current Philippine date
        /// </summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// Format for HTML date input
        /// </summary>
        public static string TodayString => Today.ToString("yyyy-MM-dd");
    }
}
