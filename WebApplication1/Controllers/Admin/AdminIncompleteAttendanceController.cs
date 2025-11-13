using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;

namespace Fingerprint_Attendance_System.Controllers.Admin
{
    [RoleAuthorize(USER_ROLES.ADMIN)]
    [Route("Admin/[controller]")]
    public class AdminIncompleteAttendanceController : Controller
    {
        private readonly MissedTimeOutService _missedTimeOutService;
        private readonly ILogger<AdminIncompleteAttendanceController> _logger;

        public AdminIncompleteAttendanceController(
            MissedTimeOutService missedTimeOutService,
            ILogger<AdminIncompleteAttendanceController> logger)
        {
            _missedTimeOutService = missedTimeOutService;
            _logger = logger;
        }

        /// <summary>
        /// View for managing incomplete attendance records
        /// </summary>
        public IActionResult Index()
        {
            return View("~/Views/Admin/IncompleteAttendance.cshtml");
        }

        /// <summary>
        /// Get all incomplete (forgot to time out) records
        /// </summary>
        [HttpGet("GetIncompleteRecords")]
        public async Task<IActionResult> GetIncompleteRecords()
        {
            try
            {
                var records = await _missedTimeOutService.GetAllIncompleteRecordsAsync();
                
                return Ok(new
                {
                    success = true,
                    data = records,
                    count = records.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incomplete records");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to retrieve incomplete records"
                });
            }
        }

        /// <summary>
        /// Manually resolve an incomplete record
        /// </summary>
        [HttpPost("ResolveRecord")]
        public async Task<IActionResult> ResolveRecord([FromBody] ResolveRecordRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Not authenticated" });
                }

                var resolved = await _missedTimeOutService.ResolveIncompleteRecordAsync(
                    request.DailyAttendId,
                    request.ManualTimeOut,
                    userId.Value);

                if (resolved)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Record resolved successfully"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to resolve record"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving incomplete record");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error resolving record"
                });
            }
        }
    }

    public class ResolveRecordRequest
    {
        public int DailyAttendId { get; set; }
        public DateTime ManualTimeOut { get; set; }
    }
}
