using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Data;

namespace Fingerprint_Attendance_System.Controllers.Student
{
    [Route("api/student")]
    [ApiController]
    [RoleAuthorize(USER_ROLES.STUDENT)]
    public class StudentDashboardApiController : ControllerBase
 {
  private readonly IStudentRepository _studentRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly DatabaseHelper _db;
        private readonly ILogger<StudentDashboardApiController> _logger;

        public StudentDashboardApiController(
  IStudentRepository studentRepo,
  IAttendanceRepository attendanceRepo,
            DatabaseHelper db,
  ILogger<StudentDashboardApiController> logger)
 {
            _studentRepo = studentRepo;
     _attendanceRepo = attendanceRepo;
    _db = db;
  _logger = logger;
     }

    /// <summary>
    /// Get personal attendance overview (doughnut chart)
      /// Returns Present, Absent, Late counts for the semester
        /// </summary>
 [HttpGet("chart/attendance-overview")]
        public async Task<IActionResult> GetAttendanceOverview()
{
   try
      {
     var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
            return Unauthorized(new { error = "User not authenticated" });

       var student = await _studentRepo.GetByUserIdAsync(userId.Value);
           if (student == null)
  return NotFound(new { error = "Student not found" });

var query = @"
  SELECT 
        COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as present_count,
 COUNT(CASE WHEN attend_status = 'absent' THEN 1 END) as absent_count,
       COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late_count
FROM attendance
     WHERE stud_id = @studId AND attend_type = 'in';";

      var parameters = new Dictionary<string, object> { { "@studId", student.StudId } };
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
    _logger.LogError(ex, "Error getting attendance overview");
     return StatusCode(500, new { error = "Failed to fetch attendance overview" });
          }
        }

        /// <summary>
        /// Get punctuality trend (line chart)
/// Returns daily attendance status for the last 14 days
        /// </summary>
        [HttpGet("chart/punctuality-trend")]
        public async Task<IActionResult> GetPunctualityTrend()
        {
            try
   {
       var userId = HttpContext.Session.GetInt32("UserId");
if (!userId.HasValue)
            return Unauthorized(new { error = "User not authenticated" });

       var student = await _studentRepo.GetByUserIdAsync(userId.Value);
      if (student == null)
         return NotFound(new { error = "Student not found" });

var query = @"
         SELECT 
        attend_date,
            COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as present,
      COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late,
      COUNT(CASE WHEN attend_status = 'absent' THEN 1 END) as absent
        FROM attendance
         WHERE stud_id = @studId 
   AND attend_date >= CURRENT_DATE - INTERVAL '14 days'
     AND attend_type = 'in'
     GROUP BY attend_date
  ORDER BY attend_date;";

      var parameters = new Dictionary<string, object> { { "@studId", student.StudId } };
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
   new { label = "On Time", data = presentData },
    new { label = "Late", data = lateData },
      new { label = "Absent", data = absentData }
    }
 });
   }
  catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting punctuality trend");
return StatusCode(500, new { error = "Failed to fetch punctuality trend" });
  }
        }

   /// <summary>
    /// Get course-wise attendance rate (bar chart)
   /// Returns attendance percentage per enrolled course
        /// </summary>
 [HttpGet("chart/course-attendance")]
        public async Task<IActionResult> GetCourseAttendance()
        {
try
     {
   var userId = HttpContext.Session.GetInt32("UserId");
 if (!userId.HasValue)
       return Unauthorized(new { error = "User not authenticated" });

   var student = await _studentRepo.GetByUserIdAsync(userId.Value);
     if (student == null)
      return NotFound(new { error = "Student not found" });

     var query = @"
   SELECT 
     co.course_code,
         COUNT(CASE WHEN a.attend_status IN ('present', 'late') THEN 1 END) as attended,
      COUNT(*) as total_sessions,
    CASE 
       WHEN COUNT(*) > 0 
     THEN ROUND((COUNT(CASE WHEN a.attend_status IN ('present', 'late') THEN 1 END)::DECIMAL / COUNT(*)) * 100)
     ELSE 0
    END as attendance_rate
            FROM enrollment e
 INNER JOIN classes c ON e.class_id = c.class_id
     INNER JOIN courses co ON c.course_id = co.course_id
   LEFT JOIN attendance a ON e.class_id = a.class_id AND e.stud_id = a.stud_id AND a.attend_type = 'in'
            WHERE e.stud_id = @studId
   GROUP BY co.course_code
ORDER BY co.course_code;";

     var parameters = new Dictionary<string, object> { { "@studId", student.StudId } };
      var dt = await _db.ExecuteQueryAsync(query, parameters);

    var labels = new List<string>();
         var data = new List<int>();

    foreach (System.Data.DataRow row in dt.Rows)
   {
labels.Add(row["course_code"]?.ToString() ?? "N/A");
     data.Add(Convert.ToInt32(row["attendance_rate"]));
      }

   return Ok(new
        {
  labels = labels,
    data = data
       });
            }
            catch (Exception ex)
 {
_logger.LogError(ex, "Error getting course attendance");
   return StatusCode(500, new { error = "Failed to fetch course attendance" });
 }
        }
    }
}
