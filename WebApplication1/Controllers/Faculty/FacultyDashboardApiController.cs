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
        /// Returns Present, Absent, Late, Excused counts for current day
        /// ✅ UPDATED: Now includes 'Excused' status from attendance table
        /// ✅ FIXED: Case-insensitive status and type matching
        /// ✅ CRITICAL FIX: All statuses (Present, Absent, Late, Excused) count all records regardless of attend_type
        /// ✅ NEW FIX: Automatic absent detection - counts students in ended classes without attendance as absent
        /// This ensures faculty manual changes to attendance status are reflected in the chart
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

                _logger.LogInformation($"📊 Today's Attendance - Faculty ID: {faculty.FacId}");

                // Check if faculty has any classes
                var classes = await _classRepo.GetFacultyClassesAsync(faculty.FacId);
                _logger.LogInformation($"📊 Faculty has {classes.Count()} classes");

                if (!classes.Any())
                {
                    // Return empty data instead of 404
                    return Ok(new
                    {
                        labels = new[] { "Present", "Absent", "Late", "Excused" },
                        data = new[] { 0, 0, 0, 0 }
                    });
                }

                // ✅ AUTOMATIC ABSENT LOGIC:
                // 1. Get all enrolled students in faculty's classes
                // 2. For classes that have ended today, check if attendance record exists
                // 3. If no record exists AND class ended -> count as absent
                var query = @"
                    WITH faculty_classes AS (
                        SELECT c.class_id
                        FROM classes c
                        WHERE c.fac_id = @facId
                    ),
                    today_day_of_week AS (
                        SELECT TO_CHAR(CURRENT_DATE, 'Day') as day_name
                    ),
                    -- Get all enrolled students with their scheduled classes for today
                    enrolled_students_today AS (
                        SELECT DISTINCT
                            e.stud_id,
                            e.class_id,
                            cs.sched_end_time
                        FROM enrollment e
                        INNER JOIN faculty_classes fc ON e.class_id = fc.class_id
                        INNER JOIN class_schedules cs ON e.class_id = cs.class_id
                        CROSS JOIN today_day_of_week dow
                        WHERE TRIM(cs.sched_day_of_week) = TRIM(dow.day_name)
                    ),
                    -- Get students with actual attendance records
                    students_with_attendance AS (
                        SELECT 
                            a.stud_id,
                            a.class_id,
                            a.attend_status
                        FROM attendance a
                        INNER JOIN faculty_classes fc ON a.class_id = fc.class_id
                        WHERE a.attend_date = CURRENT_DATE
                    ),
                    -- Determine automatic absents (students enrolled in ended classes without records)
                    automatic_absents AS (
                        SELECT DISTINCT est.stud_id
                        FROM enrolled_students_today est
                        LEFT JOIN students_with_attendance swa 
                            ON est.stud_id = swa.stud_id 
                            AND est.class_id = swa.class_id
                        WHERE swa.stud_id IS NULL  -- No attendance record
                          AND est.sched_end_time < CURRENT_TIME  -- Class has ended
                    )
                    SELECT 
                        -- Count explicit attendance records
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(swa.attend_status::TEXT) = 'present'
                            THEN swa.stud_id 
                        END) as present_count,
                        -- Count explicit absents + automatic absents
                        (
                            COUNT(DISTINCT CASE 
                                WHEN LOWER(swa.attend_status::TEXT) = 'absent'
                                THEN swa.stud_id 
                            END) + 
                            (SELECT COUNT(*) FROM automatic_absents)
                        ) as absent_count,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(swa.attend_status::TEXT) = 'late'
                            THEN swa.stud_id 
                        END) as late_count,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(swa.attend_status::TEXT) = 'excused'
                            THEN swa.stud_id 
                        END) as excused_count
                    FROM students_with_attendance swa;";

                var parameters = new Dictionary<string, object> { { "@facId", faculty.FacId } };
                var dt = await _db.ExecuteQueryAsync(query, parameters);

                int presentCount = 0, absentCount = 0, lateCount = 0, excusedCount = 0;

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    presentCount = Convert.ToInt32(row["present_count"]);
                    absentCount = Convert.ToInt32(row["absent_count"]);
                    lateCount = Convert.ToInt32(row["late_count"]);
                    excusedCount = Convert.ToInt32(row["excused_count"]);
                }

                _logger.LogInformation($"📊 Today's counts: P={presentCount}, A={absentCount} (includes automatic), L={lateCount}, E={excusedCount}");

                return Ok(new
                {
                    labels = new[] { "Present", "Absent", "Late", "Excused" },
                    data = new[]
                    {
                        presentCount,
                        absentCount,
                        lateCount,
                        excusedCount
                    }
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
        /// ✅ UPDATED: Now includes 'Excused' status from attendance table
        /// ✅ FIXED: Case-insensitive status and type matching
        /// ✅ DIAGNOSTIC: Enhanced logging to trace data flow
        /// ✅ CRITICAL FIX: All statuses (Present, Absent, Late, Excused) count all records regardless of attend_type
        /// ✅ NEW FIX: Automatic absent detection for past days - counts students in ended classes without records
        /// This ensures faculty manual changes to attendance status are reflected in the chart
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

                _logger.LogInformation($"📊 Weekly Attendance - Faculty ID: {faculty.FacId}, User ID: {userId.Value}");

                // Check if faculty has any classes
                var classes = await _classRepo.GetFacultyClassesAsync(faculty.FacId);
                _logger.LogInformation($"📊 Faculty has {classes.Count()} classes: {string.Join(", ", classes.Select(c => $"ClassID={c.ClassId}"))}");

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
                            new { label = "Absent", data = new int[] { } },
                            new { label = "Excused", data = new int[] { } }
                        }
                    });
                }

                // ✅ DIAGNOSTIC: Check what attendance records exist for this faculty
                var diagnosticQuery = @"
                    SELECT 
                        a.attend_date,
                        a.class_id,
                        c.fac_id,
                        LOWER(a.attend_status::TEXT) as attend_status,
                        LOWER(a.attend_type::TEXT) as attend_type,
                        COUNT(*) as count
                    FROM attendance a
                    INNER JOIN classes c ON a.class_id = c.class_id
                    WHERE a.attend_date >= CURRENT_DATE - INTERVAL '6 days'
                        AND a.attend_date <= CURRENT_DATE
                    GROUP BY a.attend_date, a.class_id, c.fac_id, a.attend_status, a.attend_type
                    ORDER BY a.attend_date DESC, c.fac_id, a.attend_status;";

                var diagDt = await _db.ExecuteQueryAsync(diagnosticQuery);

                _logger.LogInformation($"📊 DIAGNOSTIC - All attendance records in last 7 days ({diagDt.Rows.Count} groups):");
                foreach (System.Data.DataRow diagRow in diagDt.Rows)
                {
                    var diagFacId = diagRow["fac_id"] != DBNull.Value ? Convert.ToInt32(diagRow["fac_id"]) : -1;
                    var belongsToThisFaculty = diagFacId == faculty.FacId ? "✅ THIS FACULTY" : $"❌ Other Faculty (FacID={diagFacId})";
                    _logger.LogInformation($"  Date: {diagRow["attend_date"]}, ClassID: {diagRow["class_id"]}, Status: {diagRow["attend_status"]}, Type: {diagRow["attend_type"]}, Count: {diagRow["count"]} - {belongsToThisFaculty}");
                }

                // ✅ CRITICAL FIX: All statuses (Present, Absent, Late, Excused) now count all records
                // This ensures faculty manual status changes are reflected in the chart
                var query = @"
                    WITH date_series AS (
                        SELECT generate_series(
                            CURRENT_DATE - INTERVAL '6 days',
                            CURRENT_DATE,
                            INTERVAL '1 day'
                        )::date AS attend_date
                    ),
                    faculty_classes AS (
                        SELECT c.class_id
                        FROM classes c
                        WHERE c.fac_id = @facId
                    )
                    SELECT 
                        ds.attend_date,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(a.attend_status::TEXT) = 'present'
                            THEN a.stud_id 
                        END) as present,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(a.attend_status::TEXT) = 'late'
                            THEN a.stud_id 
                        END) as late,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(a.attend_status::TEXT) = 'absent'
                            THEN a.stud_id 
                        END) as absent,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(a.attend_status::TEXT) = 'excused'
                            THEN a.stud_id 
                        END) as excused
                    FROM date_series ds
                    CROSS JOIN faculty_classes fc
                    LEFT JOIN attendance a 
                        ON fc.class_id = a.class_id 
                        AND a.attend_date = ds.attend_date
                    GROUP BY ds.attend_date
                    ORDER BY ds.attend_date;";

                var parameters = new Dictionary<string, object> { { "@facId", faculty.FacId } };
                var dt = await _db.ExecuteQueryAsync(query, parameters);

                // ✅ AUTOMATIC ABSENT LOGIC FOR WEEKLY DATA:
                // For each day in the past 7 days:
                // 1. Get all enrolled students with classes scheduled for that day
                // 2. Check if attendance record exists
                // 3. If no record AND class ended -> count as absent
                var autoAbsentQuery = @"
                    WITH date_series AS (
                        SELECT generate_series(
                            CURRENT_DATE - INTERVAL '6 days',
                            CURRENT_DATE,
                            INTERVAL '1 day'
                        )::date AS attend_date
                    ),
                    faculty_classes AS (
                        SELECT c.class_id
                        FROM classes c
                        WHERE c.fac_id = @facId
                    ),
                    -- For each date, get enrolled students with scheduled classes
                    daily_enrolled_students AS (
                        SELECT DISTINCT
                            ds.attend_date,
                            e.stud_id,
                            e.class_id,
                            cs.sched_end_time,
                            CASE 
                                -- Check if class has ended for that date
                                WHEN ds.attend_date < CURRENT_DATE THEN TRUE  -- Past dates: all classes ended
                                WHEN ds.attend_date = CURRENT_DATE AND cs.sched_end_time < CURRENT_TIME THEN TRUE  -- Today: check time
                                ELSE FALSE
                            END as class_has_ended
                        FROM date_series ds
                        CROSS JOIN enrollment e
                        INNER JOIN faculty_classes fc ON e.class_id = fc.class_id
                        INNER JOIN class_schedules cs ON e.class_id = cs.class_id
                        WHERE TRIM(cs.sched_day_of_week) = TRIM(TO_CHAR(ds.attend_date, 'Day'))
                    ),
                    -- Get actual attendance records
                    attendance_records AS (
                        SELECT 
                            a.attend_date,
                            a.stud_id,
                            a.class_id,
                            a.attend_status
                        FROM attendance a
                        INNER JOIN faculty_classes fc ON a.class_id = fc.class_id
                        WHERE a.attend_date >= CURRENT_DATE - INTERVAL '6 days'
                          AND a.attend_date <= CURRENT_DATE
                    ),
                    -- Calculate automatic absents per day
                    daily_automatic_absents AS (
                        SELECT 
                            des.attend_date,
                            COUNT(DISTINCT des.stud_id) as auto_absent_count
                        FROM daily_enrolled_students des
                        LEFT JOIN attendance_records ar 
                            ON des.attend_date = ar.attend_date 
                            AND des.stud_id = ar.stud_id 
                            AND des.class_id = ar.class_id
                        WHERE ar.stud_id IS NULL  -- No attendance record
                          AND des.class_has_ended = TRUE  -- Class has ended
                        GROUP BY des.attend_date
                    )
                    SELECT 
                        ds.attend_date,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(ar.attend_status::TEXT) = 'present'
                            THEN ar.stud_id 
                        END) as present,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(ar.attend_status::TEXT) = 'late'
                            THEN ar.stud_id 
                        END) as late,
                        -- Combine explicit absents + automatic absents
                        (
                            COUNT(DISTINCT CASE 
                                WHEN LOWER(ar.attend_status::TEXT) = 'absent'
                                THEN ar.stud_id 
                            END) + 
                            COALESCE(daa.auto_absent_count, 0)
                        ) as absent,
                        COUNT(DISTINCT CASE 
                            WHEN LOWER(ar.attend_status::TEXT) = 'excused'
                            THEN ar.stud_id 
                        END) as excused
                    FROM date_series ds
                    LEFT JOIN attendance_records ar ON ds.attend_date = ar.attend_date
                    LEFT JOIN daily_automatic_absents daa ON ds.attend_date = daa.attend_date
                    GROUP BY ds.attend_date, daa.auto_absent_count
                    ORDER BY ds.attend_date;";

                var autoAbsentParameters = new Dictionary<string, object> { { "@facId", faculty.FacId } };
                var autoAbsentDt = await _db.ExecuteQueryAsync(autoAbsentQuery, autoAbsentParameters);

                var labels = new List<string>();
                var presentData = new List<int>();
                var lateData = new List<int>();
                var absentData = new List<int>();
                var excusedData = new List<int>();

                foreach (System.Data.DataRow row in autoAbsentDt.Rows)
                {
                    var date = Convert.ToDateTime(row["attend_date"]);
                    var presentCount = Convert.ToInt32(row["present"]);
                    var lateCount = Convert.ToInt32(row["late"]);
                    var absentCount = Convert.ToInt32(row["absent"]);
                    var excusedCount = Convert.ToInt32(row["excused"]);

                    labels.Add(date.ToString("MMM dd"));
                    presentData.Add(presentCount);
                    lateData.Add(lateCount);
                    absentData.Add(absentCount);
                    excusedData.Add(excusedCount);

                    _logger.LogInformation($"📊 {date:MMM dd}: P={presentCount}, L={lateCount}, A={absentCount} (includes automatic), E={excusedCount}");
                }

                var totalRecords = presentData.Sum() + lateData.Sum() + absentData.Sum() + excusedData.Sum();
                _logger.LogInformation($"📊 Returning data: Labels={labels.Count}, Total Records={totalRecords} (Present={presentData.Sum()}, Late={lateData.Sum()}, Absent={absentData.Sum()}, Excused={excusedData.Sum()})");

                return Ok(new
                {
                    labels = labels,
                    datasets = new object[]
                    {
                        new { label = "Present", data = presentData },
                        new { label = "Late", data = lateData },
                        new { label = "Absent", data = absentData },
                        new { label = "Excused", data = excusedData }
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

                // ✅ FIXED: Use attendance table's attend_status to properly count Present vs Late
                var query = @"
                    SELECT 
                        COUNT(DISTINCT CASE 
                            WHEN a.attend_status = 'present'::attendance_status 
                            THEN a.stud_id 
                        END) as on_time,
                        COUNT(DISTINCT CASE 
                            WHEN a.attend_status = 'late'::attendance_status 
                            THEN a.stud_id 
                        END) as late
                    FROM attendance a
                    INNER JOIN classes c ON a.class_id = c.class_id
                    WHERE c.fac_id = @facId
                      AND a.attend_date >= CURRENT_DATE - INTERVAL '7 days'
                      AND a.attend_status IN ('present'::attendance_status, 'late'::attendance_status);";

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
