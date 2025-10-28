using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Data;

namespace Fingerprint_Attendance_System.Controllers.Admin
{
    [Route("api/admin")]
    [ApiController]
    [RoleAuthorize(USER_ROLES.ADMIN)]
    public class AdminDashboardApiController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly ICourseRepository _courseRepo;
   private readonly IFacultyRepository _facultyRepo;
        private readonly IRoomRepository _roomRepo;
   private readonly IAdministrationAuditRepository _auditRepo;
        private readonly FingerprintService _fingerprintService;
        private readonly DatabaseHelper _db;
        private readonly ILogger<AdminDashboardApiController> _logger;

        public AdminDashboardApiController(
            IUserRepository userRepo,
          ICourseRepository courseRepo,
     IFacultyRepository facultyRepo,
            IRoomRepository roomRepo,
     IAdministrationAuditRepository auditRepo,
     FingerprintService fingerprintService,
  DatabaseHelper db,
   ILogger<AdminDashboardApiController> logger)
        {
     _userRepo = userRepo;
            _courseRepo = courseRepo;
        _facultyRepo = facultyRepo;
    _roomRepo = roomRepo;
          _auditRepo = auditRepo;
   _fingerprintService = fingerprintService;
    _db = db;
       _logger = logger;
     }

        /// <summary>
        /// Get real-time system statistics
  /// </summary>
    [HttpGet("stats")]
      public async Task<IActionResult> GetSystemStats()
        {
 try
            {
       var users = await _userRepo.GetAllAsync();
          var courses = await _courseRepo.GetAllAsync();
                var faculty = await _facultyRepo.GetAllAsync();
  var rooms = await _roomRepo.GetAllAsync();

  return Ok(new
       {
       totalUsers = users.Count(),
  totalCourses = courses.Count(),
         totalFaculty = faculty.Count(),
         totalRooms = rooms.Count()
              });
    }
      catch (Exception ex)
            {
    _logger.LogError(ex, "Error getting system stats");
                return StatusCode(500, new { error = "Failed to fetch system stats" });
            }
        }

        /// <summary>
    /// Get real-time system status (database and biometric scanner)
        /// </summary>
        [HttpGet("system-status")]
        public async Task<IActionResult> GetSystemStatus()
  {
          try
            {
       // Check database connection
         bool isDatabaseOnline = false;
     try
    {
            var testQuery = "SELECT 1;";
   var result = await _db.ExecuteQueryAsync(testQuery);
     isDatabaseOnline = result != null;
 }
                catch
         {
           isDatabaseOnline = false;
          }

    // Check biometric scanner status
      bool isScannerOnline = false;
        string scannerStatus = "Offline";
try
        {
      var status = _fingerprintService.GetStatus();
          isScannerOnline = status == "Device Connected";
    scannerStatus = isScannerOnline ? "Online" : status;
              }
                catch
       {
       isScannerOnline = false;
     scannerStatus = "Error";
         }

        return Ok(new
           {
   database = new
          {
       isOnline = isDatabaseOnline,
              status = isDatabaseOnline ? "Online" : "Offline"
     },
      scanner = new
     {
        isOnline = isScannerOnline,
  status = scannerStatus
        }
                });
          }
  catch (Exception ex)
        {
       _logger.LogError(ex, "Error getting system status");
          return StatusCode(500, new { error = "Failed to fetch system status" });
          }
   }

        /// <summary>
  /// Get recent activity from administration_audit table only (unified source)
        /// Returns top 3 most recent audit log entries
        /// </summary>
        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 3)
        {
            try
        {
          // Fetch all audit logs from administration_audit table
       var allActivities = await _auditRepo.GetAllAsync();
       
         // Get top 3 (or specified limit) most recent activities, sorted by newest first
     var activities = allActivities
  .OrderByDescending(a => a.AudActionTime)
        .Take(limit)
       .Select(a => new
         {
          action = a.AudAction,
       details = a.AudDetails ?? "No details",
               timestamp = a.AudActionTime
    })
          .ToList();

     return Ok(new { activities = activities });
            }
          catch (Exception ex)
  {
        _logger.LogError(ex, "Error getting recent activity");
     return StatusCode(500, new { error = "Failed to fetch recent activity" });
            }
      }
    }
}
