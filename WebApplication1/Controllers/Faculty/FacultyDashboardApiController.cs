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
        private readonly IClassRepository _classRepo;
        private readonly DatabaseHelper _db;
        private readonly ILogger<FacultyDashboardApiController> _logger;

        public FacultyDashboardApiController(
            IFacultyRepository facultyRepo,
            IAttendanceRepository attendanceRepo,
            IClassRepository classRepo,
            DatabaseHelper db,
            ILogger<FacultyDashboardApiController> logger)
        {
            _facultyRepo = facultyRepo;
            _attendanceRepo = attendanceRepo;
            _classRepo = classRepo;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Get today's attendance status for faculty's classes (doughnut chart)
        /// Returns Present, Absent, Late counts for current day
        /// ✅ CRITICAL FIX: Removed class_id filter - old records have NULL class_id
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
                    return Unauthorized(new { error = "Faculty not found" });

                // Check if faculty has any classes
                var classes = await _classRepo.GetFacultyClassesAsync(faculty.FacId);
                if (!classes.Any())
                {
                    // Return empty data instead of 404
                    return Ok(new
                    {
                        labels = new[] { "Present", "Absent", "Late" },
                        data = new[] { 0, 0, 0 }
                    });
                }

                // ✅ CRITICAL FIX: Match on student and date only (not class_id)
                // Old records in daily_attendance_log have NULL class_id
                var query = @"
                    WITH faculty_students AS (
                        SELECT DISTINCT e.stud_id
                        FROM enrollment e
                        INNER JOIN classes c ON e.class_id = c.class_id
                        WHERE c.fac_id = @facId
                    )
                    SELECT 
                        COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NOT NULL AND dal.time_out IS NOT NULL 
                            THEN dal.stud_id 
                        END) as present_count,
                        COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NULL 
                            THEN fs.stud_id 
                        END) as absent_count,
                        COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NOT NULL 
                            THEN dal.stud_id 
                        END) - COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NOT NULL AND dal.time_out IS NOT NULL 
                            THEN dal.stud_id 
                        END) as late_count
                    FROM faculty_students fs
                    LEFT JOIN daily_attendance_log dal 
                        ON fs.stud_id = dal.stud_id 
                        AND dal.attend_date = CURRENT_DATE;";

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
        /// ✅ CRITICAL FIX: Removed class_id filter - old records have NULL class_id
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
                    return Unauthorized(new { error = "Faculty not found" });

                // Check if faculty has any classes
                var classes = await _classRepo.GetFacultyClassesAsync(faculty.FacId);
                if (!classes.Any())
                {
                    // Return empty data instead of 404
                    return Ok(new
                    {
                        labels = new string[] { },
                        datasets = new object[]
                        {
                            new { label = "Present", data = new int[] { } },
                            new { label = "Late", data = new int[] { } },
                            new { label = "Absent", data = new int[] { } }
                        }
                    });
                }

                // ✅ CRITICAL FIX: Match on student and date only
                var query = @"
                    WITH date_series AS (
                        SELECT generate_series(
                            CURRENT_DATE - INTERVAL '6 days',
                            CURRENT_DATE,
                            INTERVAL '1 day'
                        )::date AS attend_date
                    ),
                    faculty_students AS (
                        SELECT DISTINCT e.stud_id
                        FROM enrollment e
                        INNER JOIN classes c ON e.class_id = c.class_id
                        WHERE c.fac_id = @facId
                    )
                    SELECT 
                        ds.attend_date,
                        COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NOT NULL AND dal.time_out IS NOT NULL 
                            THEN dal.stud_id 
                        END) as present,
                        COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NOT NULL 
                            THEN dal.stud_id 
                        END) - COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NOT NULL AND dal.time_out IS NOT NULL 
                            THEN dal.stud_id 
                        END) as late,
                        COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NULL 
                            THEN fs.stud_id 
                        END) as absent
                    FROM date_series ds
                    CROSS JOIN faculty_students fs
                    LEFT JOIN daily_attendance_log dal 
                        ON fs.stud_id = dal.stud_id 
                        AND dal.attend_date = ds.attend_date
                    GROUP BY ds.attend_date
                    ORDER BY ds.attend_date;";

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
        /// ✅ CRITICAL FIX: Removed class_id filter - old records have NULL class_id
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
                    return Unauthorized(new { error = "Faculty not found" });

                // Check if faculty has any classes
                var classes = await _classRepo.GetFacultyClassesAsync(faculty.FacId);
                if (!classes.Any())
                {
                    // Return empty data instead of 404
                    return Ok(new
                    {
                        labels = new[] { "On Time", "Late" },
                        data = new[] { 0, 0 }
                    });
                }

                // ✅ CRITICAL FIX: Match on student and date only
                var query = @"
                    WITH faculty_students AS (
                        SELECT DISTINCT e.stud_id
                        FROM enrollment e
                        INNER JOIN classes c ON e.class_id = c.class_id
                        WHERE c.fac_id = @facId
                    )
                    SELECT 
                        COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NOT NULL AND dal.time_out IS NOT NULL 
                            THEN dal.stud_id 
                        END) as on_time,
                        COUNT(DISTINCT CASE 
                            WHEN dal.time_in IS NOT NULL AND dal.time_out IS NULL 
                            THEN dal.stud_id 
                        END) as late
                    FROM faculty_students fs
                    LEFT JOIN daily_attendance_log dal 
                        ON fs.stud_id = dal.stud_id 
                        AND dal.attend_date >= CURRENT_DATE - INTERVAL '7 days';";

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
