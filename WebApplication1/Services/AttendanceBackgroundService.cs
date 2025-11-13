namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Background service that runs absence detection at end of day
    /// Runs daily at 11:59 PM to mark absent students
    /// STEP 11: Automated Absence Marking
    /// </summary>
    public class AttendanceBackgroundService : BackgroundService
    {
        private readonly ILogger<AttendanceBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public AttendanceBackgroundService(
            ILogger<AttendanceBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("? Attendance Background Service started");

            // Calculate time until 11:59 PM today
            var now = DateTime.Now;
            var targetTime = DateTime.Today.AddHours(23).AddMinutes(59);
            
            if (now > targetTime)
            {
                // If it's already past 11:59 PM, schedule for tomorrow
                targetTime = targetTime.AddDays(1);
            }

            var initialDelay = targetTime - now;
            _logger.LogInformation($"? Absence detection will run at {targetTime:yyyy-MM-dd HH:mm:ss} (in {initialDelay.TotalHours:F1} hours)");

            // Wait until initial target time
            try
            {
                await Task.Delay(initialDelay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Attendance Background Service cancelled during initial delay");
                return;
            }

            // Run absence detection immediately
            await RunAbsenceDetectionAsync();

            // Then schedule to run daily at 11:59 PM
            var period = TimeSpan.FromHours(24);
            _timer = new Timer(
                callback: async _ => await RunAbsenceDetectionAsync(),
                state: null,
                dueTime: TimeSpan.Zero,
                period: period);

            _logger.LogInformation("? Absence detection scheduled to run daily at 11:59 PM");

            // Keep the service running
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Attendance Background Service cancelled");
            }
        }

        private async Task RunAbsenceDetectionAsync()
        {
            try
            {
                _logger.LogInformation("=== Starting Scheduled Absence Detection ===");

                using var scope = _serviceProvider.CreateScope();
                var absenceService = scope.ServiceProvider.GetRequiredService<AbsenceDetectionService>();

                // Process absences for today
                await absenceService.ProcessAbsencesForTodayAsync();

                _logger.LogInformation("=== Scheduled Absence Detection Complete ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error running scheduled absence detection");
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
