using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;

namespace Fingerprint_Attendance_System.Controllers.Admin
{
    /// <summary>
    /// Admin controller for manual attendance operations
    /// Allows manual triggering of absence detection for testing or missed runs
    /// </summary>
    [RoleAuthorize(USER_ROLES.ADMIN)]
    [Route("Admin/[controller]")]
    public class AdminAttendanceController : Controller
    {
        private readonly AbsenceDetectionService _absenceService;
        private readonly ILogger<AdminAttendanceController> _logger;

        public AdminAttendanceController(
            AbsenceDetectionService absenceService,
            ILogger<AdminAttendanceController> logger)
        {
            _absenceService = absenceService;
            _logger = logger;
        }

        /// <summary>
        /// Manual trigger for absence detection (for testing or end-of-day runs)
        /// POST: /Admin/AdminAttendance/ProcessAbsences?date=2025-01-11
        /// </summary>
        [HttpPost("ProcessAbsences")]
        public async Task<IActionResult> ProcessAbsences([FromQuery] string? date = null)
        {
            try
            {
                DateTime targetDate = string.IsNullOrEmpty(date)
                    ? DateTime.Today
                    : DateTime.Parse(date);

                _logger.LogInformation($"?? Manual absence detection triggered for {targetDate:yyyy-MM-dd}");

                await _absenceService.ProcessAbsencesForDateAsync(targetDate);

                return Ok(new
                {
                    success = true,
                    message = $"Absence detection completed for {targetDate:yyyy-MM-dd}",
                    date = targetDate.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error processing absences manually");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing absences",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get status of background service
        /// GET: /Admin/AdminAttendance/Status
        /// </summary>
        [HttpGet("Status")]
        public IActionResult GetStatus()
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = DateTime.Today.AddHours(23).AddMinutes(59);
                
                if (now > nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                return Ok(new
                {
                    success = true,
                    currentTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
                    nextScheduledRun = nextRun.ToString("yyyy-MM-dd HH:mm:ss"),
                    hoursUntilNextRun = (nextRun - now).TotalHours
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting background service status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting status",
                    error = ex.Message
                });
            }
        }
    }
}
