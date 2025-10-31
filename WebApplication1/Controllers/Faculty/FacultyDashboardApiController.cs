using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Data;

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    [Route("api/faculty")]
    [ApiController]
    [RoleAuthorize(USER_ROLES.FACULTY)]
    public class FacultyDashboardApiController : ControllerBase
    {
        private readonly IFacultyRepository _facultyRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly DatabaseHelper _db;
     private readonly ILogger<FacultyDashboardApiController> _logger;

        public FacultyDashboardApiController(
 IFacultyRepository facultyRepo,
   IAttendanceRepository attendanceRepo,
          DatabaseHelper db,
       ILogger<FacultyDashboardApiController> logger)
{
    _facultyRepo = facultyRepo;
       _attendanceRepo = attendanceRepo;
         _db = db;
        _logger = logger;
        }

   /// <summary>
        /// Get today's attendance status for faculty's classes (doughnut chart)
        /// Returns Present, Absent, Late counts for current day
        /// </summary>
        [HttpGet("chart/today-attendance")]
        public async Task<IActionResult> GetTodayAttendanceStatus()
        {
         try
          {
    var userId = HttpContext.Session.GetInt32("UserId");
          if (!userId.HasValue)
        return Unauthorized(new { error = "User not authenticated" });

              var faculty = await _facultyRepo.GetByUserIdAsync(userId.Value);
       if (faculty == null)
      return NotFound(new { error = "Faculty not found" });

                var query = @"
         SELECT 
        COUNT(CASE WHEN a.attend_status = 'present' THEN 1 END) as present_count,
   COUNT(CASE WHEN a.attend_status = 'absent' THEN 1 END) as absent_count,
       COUNT(CASE WHEN a.attend_status = 'late' THEN 1 END) as late_count
         FROM attendance a
 INNER JOIN classes c ON a.class_id = c.class_id
          WHERE c.fac_id = @facId 
           AND a.attend_date = CURRENT_DATE 
    AND a.attend_type = 'in';";

     var parameters = new Dictionary<string, object> { { "@facId", faculty.FacId } };
          var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count > 0)
     {
   var row = dt.Rows[0];
   return Ok(new
      {
   labels = new[] { "Present", "Absent", "Late" },
  data = new[] 
        { 
    Convert.ToInt32(row["present_count"]),
                Convert.ToInt32(row["absent_count"]),
   Convert.ToInt32(row["late_count"])
            }
          });
    }

    return Ok(new
                {
    labels = new[] { "Present", "Absent", "Late" },
                    data = new[] { 0, 0, 0 }
    });
  }
            catch (Exception ex)
            {
     _logger.LogError(ex, "Error getting today's attendance status");
      return StatusCode(500, new { error = "Failed to fetch today's attendance" });
            }
        }

  /// <summary>
        /// Get weekly attendance trend for faculty's classes (line chart)
        /// Returns attendance data for the last 7 days
        /// </summary>
        [HttpGet("chart/weekly-attendance")]
     public async Task<IActionResult> GetWeeklyAttendance()
     {
     try
 {
   var userId = HttpContext.Session.GetInt32("UserId");
  if (!userId.HasValue)
            return Unauthorized(new { error = "User not authenticated" });

      var faculty = await _facultyRepo.GetByUserIdAsync(userId.Value);
                if (faculty == null)
       return NotFound(new { error = "Faculty not found" });

 var query = @"
      SELECT 
           a.attend_date,
   COUNT(CASE WHEN a.attend_status = 'present' THEN 1 END) as present,
     COUNT(CASE WHEN a.attend_status = 'late' THEN 1 END) as late,
   COUNT(CASE WHEN a.attend_status = 'absent' THEN 1 END) as absent
        FROM attendance a
        INNER JOIN classes c ON a.class_id = c.class_id
    WHERE c.fac_id = @facId 
              AND a.attend_date >= CURRENT_DATE - INTERVAL '7 days'
            AND a.attend_type = 'in'
   GROUP BY a.attend_date
     ORDER BY a.attend_date;";

     var parameters = new Dictionary<string, object> { { "@facId", faculty.FacId } };
          var dt = await _db.ExecuteQueryAsync(query, parameters);

    var labels = new List<string>();
var presentData = new List<int>();
                var lateData = new List<int>();
    var absentData = new List<int>();

    foreach (System.Data.DataRow row in dt.Rows)
{
        var date = Convert.ToDateTime(row["attend_date"]);
     labels.Add(date.ToString("MMM dd"));
            presentData.Add(Convert.ToInt32(row["present"]));
              lateData.Add(Convert.ToInt32(row["late"]));
    absentData.Add(Convert.ToInt32(row["absent"]));
    }

 return Ok(new
    {
    labels = labels,
         datasets = new object[]
      {
          new { label = "Present", data = presentData },
         new { label = "Late", data = lateData },
    new { label = "Absent", data = absentData }
     }
      });
            }
    catch (Exception ex)
            {
   _logger.LogError(ex, "Error getting weekly attendance");
                return StatusCode(500, new { error = "Failed to fetch weekly attendance" });
            }
        }

        /// <summary>
        /// Get student punctuality analysis (bar chart)
    /// Returns on-time vs late arrivals for the last 7 days
        /// </summary>
        [HttpGet("chart/punctuality")]
        public async Task<IActionResult> GetPunctualityAnalysis()
        {
            try
            {
     var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
               return Unauthorized(new { error = "User not authenticated" });

     var faculty = await _facultyRepo.GetByUserIdAsync(userId.Value);
  if (faculty == null)
                    return NotFound(new { error = "Faculty not found" });

     var query = @"
SELECT 
  COUNT(CASE WHEN a.attend_status = 'present' THEN 1 END) as on_time,
             COUNT(CASE WHEN a.attend_status = 'late' THEN 1 END) as late
      FROM attendance a
           INNER JOIN classes c ON a.class_id = c.class_id
         WHERE c.fac_id = @facId 
     AND a.attend_date >= CURRENT_DATE - INTERVAL '7 days'
AND a.attend_type = 'in';";

             var parameters = new Dictionary<string, object> { { "@facId", faculty.FacId } };
     var dt = await _db.ExecuteQueryAsync(query, parameters);

    if (dt.Rows.Count > 0)
        {
     var row = dt.Rows[0];
                    return Ok(new
 {
             labels = new[] { "On Time", "Late" },
       data = new[] 
         { 
     Convert.ToInt32(row["on_time"]),
              Convert.ToInt32(row["late"])
      }
         });
         }

      return Ok(new
    {
 labels = new[] { "On Time", "Late" },
        data = new[] { 0, 0 }
 });
  }
       catch (Exception ex)
  {
              _logger.LogError(ex, "Error getting punctuality analysis");
      return StatusCode(500, new { error = "Failed to fetch punctuality data" });
            }
      }
    }
}
