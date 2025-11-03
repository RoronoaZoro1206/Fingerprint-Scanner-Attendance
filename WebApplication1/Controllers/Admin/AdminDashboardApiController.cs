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

      /// <summary>
    /// Get user role distribution for pie/doughnut chart
/// Returns count of Admin, Faculty, and Student users
/// </summary>
[HttpGet("chart/user-roles")]
public async Task<IActionResult> GetUserRoleDistribution()
{
    try
    {
        _logger.LogInformation("GetUserRoleDistribution - Starting...");

        // ? FIXED: Corrected SQL query to match actual database schema
  // users.role_id directly references user_roles.role_id (no junction table)
        var query = @"
            SELECT ur.role_name, COUNT(u.user_id) as count
            FROM user_roles ur
   LEFT JOIN users u ON ur.role_id = u.role_id
      WHERE ur.role_name IN ('admin', 'faculty', 'student')
GROUP BY ur.role_name, ur.role_id
   ORDER BY ur.role_id;";
        
      var dt = await _db.ExecuteQueryAsync(query);
    
        _logger.LogInformation($"GetUserRoleDistribution - Query returned {dt?.Rows.Count ?? 0} rows");
      
        var labels = new List<string>();
        var data = new List<int>();
      
        if (dt != null && dt.Rows.Count > 0)
        {
            foreach (System.Data.DataRow row in dt.Rows)
        {
                var roleName = row["role_name"]?.ToString() ?? "Unknown";
      var count = Convert.ToInt32(row["count"]);
  
         // Capitalize first letter
         labels.Add(char.ToUpper(roleName[0]) + roleName.Substring(1));
data.Add(count);
          
     _logger.LogInformation($"GetUserRoleDistribution - Role: {roleName}, Count: {count}");
 }
        }
  
        // Always return data, even if empty
        if (labels.Count == 0)
        {
 _logger.LogWarning("GetUserRoleDistribution - No data found, returning default roles with 0 count");
  labels = new List<string> { "Admin", "Faculty", "Student" };
          data = new List<int> { 0, 0, 0 };
        }
        
        return Ok(new
        {
   labels = labels,
            data = data
 });
    }
 catch (Exception ex)
    {
        _logger.LogError(ex, $"Error getting user role distribution: {ex.Message}");
        // Return empty data instead of error to prevent chart failure
     return Ok(new
        {
   labels = new List<string> { "Admin", "Faculty", "Student" },
  data = new List<int> { 0, 0, 0 }
        });
    }
}

  /// <summary>
      /// Get daily attendance trend for last 30 days (line chart)
/// Returns date and count of attendance records per day
        /// </summary>
        [HttpGet("chart/attendance-trend")]
        public async Task<IActionResult> GetAttendanceTrend([FromQuery] int days = 30)
        {
            try
            {
                var query = @"
    SELECT attend_date, COUNT(*) as count
         FROM attendance
         WHERE attend_date >= CURRENT_DATE - INTERVAL '" + days + @" days'
        GROUP BY attend_date
    ORDER BY attend_date;";
          
     var dt = await _db.ExecuteQueryAsync(query);
       
    var labels = new List<string>();
    var data = new List<int>();
     
          foreach (System.Data.DataRow row in dt.Rows)
       {
         var date = Convert.ToDateTime(row["attend_date"]);
        labels.Add(date.ToString("MMM dd"));
        data.Add(Convert.ToInt32(row["count"]));
        }
         
         return Ok(new
            {
         labels = labels,
         data = data
    });
            }
        catch (Exception ex)
     {
     _logger.LogError(ex, "Error getting attendance trend");
      return StatusCode(500, new { error = "Failed to fetch attendance trend" });
      }
        }

        /// <summary>
        /// Get fingerprint registration status (bar chart)
        /// Returns users with and without registered fingerprints
        /// </summary>
        [HttpGet("chart/fingerprint-status")]
        public async Task<IActionResult> GetFingerprintStatus()
        {
         try
            {
      var query = @"
     SELECT 
  COUNT(CASE WHEN ft.ftemp_id IS NOT NULL THEN 1 END) as registered,
    COUNT(CASE WHEN ft.ftemp_id IS NULL THEN 1 END) as not_registered
           FROM users u
               LEFT JOIN fingerprint_templates ft ON u.user_id = ft.user_id;";
    
   var dt = await _db.ExecuteQueryAsync(query);
                
         if (dt.Rows.Count > 0)
      {
   var row = dt.Rows[0];
        return Ok(new
         {
      labels = new[] { "Registered", "Not Registered" },
data = new[] 
           { 
        Convert.ToInt32(row["registered"]), 
        Convert.ToInt32(row["not_registered"]) 
        }
  });
        }
   
  return Ok(new
            {
            labels = new[] { "Registered", "Not Registered" },
               data = new[] { 0, 0 }
      });
   }
   catch (Exception ex)
   {
     _logger.LogError(ex, "Error getting fingerprint status");
          return StatusCode(500, new { error = "Failed to fetch fingerprint status" });
            }
        }
    }
}
