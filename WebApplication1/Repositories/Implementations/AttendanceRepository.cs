using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class AttendanceRepository : IAttendanceRepository
{
    private readonly DatabaseHelper _db;

    public AttendanceRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Attendance>> GetAllAsync()
    {
        string query = @"SELECT attend_id, class_id, stud_id, attend_date, attend_scan_time, attend_type, attend_status, 
                         attend_remark, attend_created_at FROM attendance;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Attendance
        {
            AttendId = Convert.ToInt32(row["attend_id"]),
            ClassId = row["class_id"] != DBNull.Value ? Convert.ToInt32(row["class_id"]) : (int?)null,
            StudId = row["stud_id"] != DBNull.Value ? Convert.ToInt32(row["stud_id"]) : (int?)null,
            AttendDate = Convert.ToDateTime(row["attend_date"]),
            AttendScanTime = Convert.ToDateTime(row["attend_scan_time"]),
            AttendType = Enum.Parse<ScanType>(row["attend_type"].ToString() ?? "In", true),
            AttendStatus = Enum.Parse<AttendanceStatus>(row["attend_status"].ToString() ?? "Pending", true),
            AttendRemark = row["attend_remark"]?.ToString(),
            AttendCreatedAt = Convert.ToDateTime(row["attend_created_at"])
        });
    }

    public async Task<Attendance?> GetByIdAsync(int attendId)
    {
        string query = @"SELECT attend_id, class_id, stud_id, attend_date, attend_scan_time, attend_type, attend_status, 
                         attend_remark, attend_created_at FROM attendance WHERE attend_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", attendId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Attendance
        {
            AttendId = Convert.ToInt32(row["attend_id"]),
            ClassId = row["class_id"] != DBNull.Value ? Convert.ToInt32(row["class_id"]) : (int?)null,
            StudId = row["stud_id"] != DBNull.Value ? Convert.ToInt32(row["stud_id"]) : (int?)null,
            AttendDate = Convert.ToDateTime(row["attend_date"]),
            AttendScanTime = Convert.ToDateTime(row["attend_scan_time"]),
            AttendType = Enum.Parse<ScanType>(row["attend_type"].ToString() ?? "In", true),
            AttendStatus = Enum.Parse<AttendanceStatus>(row["attend_status"].ToString() ?? "Pending", true),
            AttendRemark = row["attend_remark"]?.ToString(),
            AttendCreatedAt = Convert.ToDateTime(row["attend_created_at"])
        };
    }

    public async Task<bool> AddAsync(Attendance attendance)
    {
        string query = @"INSERT INTO attendance (class_id, stud_id, attend_date, attend_scan_time, attend_type, attend_status, attend_remark, attend_created_at)
                         VALUES (@class, @stud, @date, @scantime, @type::scan_type_enum, @status::attendance_status, @remark, @created);";
        var parameters = new Dictionary<string, object>
        {
            { "@class", attendance.ClassId ?? (object)DBNull.Value },
            { "@stud", attendance.StudId ?? (object)DBNull.Value },
            { "@date", attendance.AttendDate },
            { "@scantime", attendance.AttendScanTime },
            { "@type", attendance.AttendType.ToString().ToLower() },
            { "@status", attendance.AttendStatus.ToString().ToLower() },
            { "@remark", attendance.AttendRemark ?? (object)DBNull.Value },
            { "@created", attendance.AttendCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Attendance attendance)
    {
        string query = @"UPDATE attendance 
                         SET class_id = @class, stud_id = @stud, attend_date = @date, attend_scan_time = @scantime,
                             attend_type = @type::scan_type_enum, attend_status = @status::attendance_status, attend_remark = @remark,
                             attend_created_at = @created
                         WHERE attend_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", attendance.AttendId },
            { "@class", attendance.ClassId ?? (object)DBNull.Value },
            { "@stud", attendance.StudId ?? (object)DBNull.Value },
            { "@date", attendance.AttendDate },
            { "@scantime", attendance.AttendScanTime },
            { "@type", attendance.AttendType.ToString().ToLower() },
            { "@status", attendance.AttendStatus.ToString().ToLower() },
            { "@remark", attendance.AttendRemark ?? (object)DBNull.Value },
            { "@created", attendance.AttendCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int attendId)
    {
        string query = "DELETE FROM attendance WHERE attend_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", attendId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int attendId)
    {
        string query = "SELECT COUNT(*) FROM attendance WHERE attend_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", attendId } };
        return await _db.RecordExistsAsync(query, parameters);
    }

    /// <summary>
    /// Gets class attendance with all student details in a single optimized query
    /// ✅ CRITICAL FIX: Removed class_id filter from daily_attendance_log JOIN
    /// - Old records in daily_attendance_log have NULL class_id
    /// - Query now matches on stud_id and date only
    /// - Shows ALL enrolled students regardless of attendance status
    /// Eliminates N+1 query problem by using JOINs and aggregation
    /// Verifies faculty ownership of the class
    /// </summary>
    public async Task<ClassAttendanceDto?> GetClassAttendanceWithDetailsAsync(int classId, DateTime date, int facId)
    {
        string query = @"
            -- First verify faculty owns this class
            WITH class_check AS (
                SELECT class_id, course_id
                FROM classes
                WHERE class_id = @classId AND fac_id = @facId
            ),
            -- ✅ CRITICAL FIX: Get all enrolled students with their daily attendance
            -- Removed class_id filter - old records have NULL class_id
            student_attendance AS (
                SELECT 
                    s.stud_id,
                    s.stud_number,
                    CONCAT(u.user_first_name, ' ', u.user_last_name) as student_name,
                    COALESCE(u.user_profile_image, 'default_profile.png') as user_profile_image,
                    dal.time_in,
                    dal.time_out,
                    dal.daily_attend_id,
                    -- Determine status based on daily attendance log
                    CASE 
                        WHEN dal.time_in IS NULL THEN 'absent'
                        WHEN dal.time_out IS NOT NULL THEN 'present'
                        WHEN dal.status = 'active' THEN 'present'
                        ELSE 'absent'
                    END as status
                FROM enrollment e
                INNER JOIN students s ON e.stud_id = s.stud_id
                INNER JOIN users u ON s.user_id = u.user_id
                -- ✅ CRITICAL FIX: Match on student and date only
                LEFT JOIN daily_attendance_log dal 
                    ON s.stud_id = dal.stud_id 
                    AND dal.attend_date = @date
                WHERE e.class_id = @classId
                ORDER BY s.stud_number
            )
            -- Combine course info with student attendance
            SELECT 
                co.course_code,
                co.course_name,
                @date::date as date,
                sa.stud_id,
                sa.stud_number,
                sa.student_name,
                sa.user_profile_image,
                sa.time_in,
                sa.time_out,
                sa.status,
                sa.daily_attend_id as attend_id
            FROM class_check cc
            INNER JOIN courses co ON cc.course_id = co.course_id
            CROSS JOIN student_attendance sa;";
        
        var parameters = new Dictionary<string, object> 
        { 
            { "@classId", classId },
            { "@date", date.Date },
            { "@facId", facId }
        };
        
        var dt = await _db.ExecuteQueryAsync(query, parameters);
        
        if (dt.Rows.Count == 0) return null;
        
        // Get course info from first row
        var firstRow = dt.Rows[0];
        var result = new ClassAttendanceDto
        {
            CourseCode = firstRow["course_code"]?.ToString() ?? "N/A",
            CourseName = firstRow["course_name"]?.ToString() ?? "N/A",
            Date = date.ToString("M/d/yyyy"),
            Students = new List<StudentAttendanceDto>()
        };
        
        // Map all student attendance records
        foreach (DataRow row in dt.Rows)
        {
            var timeIn = row["time_in"] != DBNull.Value 
                ? Convert.ToDateTime(row["time_in"]).ToString("hh:mm:ss tt") 
                : null;
            
            var timeOut = row["time_out"] != DBNull.Value 
                ? Convert.ToDateTime(row["time_out"]).ToString("hh:mm:ss tt") 
                : null;
            
            result.Students.Add(new StudentAttendanceDto
            {
                StudId = Convert.ToInt32(row["stud_id"]),
                StudentNumber = row["stud_number"]?.ToString() ?? "",
                Name = row["student_name"]?.ToString() ?? "Unknown",
                ProfileImage = row["user_profile_image"]?.ToString() ?? "default_profile.png",
                TimeIn = timeIn,
                TimeOut = timeOut,
                Status = row["status"]?.ToString() ?? "absent",
                AttendId = row["attend_id"] != DBNull.Value ? Convert.ToInt32(row["attend_id"]) : null
            });
        }
        
        return result;
    }

    public async Task<IEnumerable<FacultyAttendanceRecordDto>> GetFacultyAttendanceRecordsAsync(int facId, int limit = 20)
    {
        string query = @"
            -- ✅ CRITICAL FIX: Removed class_id filter - old records have NULL class_id
            -- Match on student and date only
            WITH faculty_students AS (
                SELECT DISTINCT e.stud_id, e.class_id, c.course_id
                FROM enrollment e
                INNER JOIN classes c ON e.class_id = c.class_id
                WHERE c.fac_id = @facId
            )
            SELECT 
                dal.attend_date,
                COALESCE(co.course_code, 'N/A') AS course_code,
                COUNT(DISTINCT CASE 
                    WHEN dal.time_in IS NOT NULL AND dal.time_out IS NOT NULL 
                    THEN dal.stud_id 
                END) AS present,
                COUNT(DISTINCT CASE 
                    WHEN dal.time_in IS NOT NULL AND dal.time_out IS NULL 
                    THEN dal.stud_id 
                END) AS late,
                COUNT(DISTINCT CASE 
                    WHEN dal.time_in IS NULL 
                    THEN fs.stud_id 
                END) AS absent,
                0 AS excused
            FROM (
                SELECT DISTINCT attend_date
                FROM daily_attendance_log
                WHERE attend_date >= CURRENT_DATE - INTERVAL '30 days'
                ORDER BY attend_date DESC
                LIMIT @limit
            ) dates
            CROSS JOIN faculty_students fs
            LEFT JOIN daily_attendance_log dal 
                ON fs.stud_id = dal.stud_id 
                AND dal.attend_date = dates.attend_date
            LEFT JOIN courses co ON fs.course_id = co.course_id
            GROUP BY dal.attend_date, co.course_code
            ORDER BY dal.attend_date DESC
            LIMIT @limit;";

        var parameters = new Dictionary<string, object> 
        { 
            { "@facId", facId },
            { "@limit", limit }
        };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new FacultyAttendanceRecordDto
        {
            Date = row["attend_date"] != DBNull.Value 
                ? Convert.ToDateTime(row["attend_date"]).ToString("yyyy-MM-dd") 
                : DateTime.Now.ToString("yyyy-MM-dd"),
            CourseCode = row["course_code"]?.ToString() ?? "N/A",
            Present = Convert.ToInt32(row["present"]),
            Late = Convert.ToInt32(row["late"]),
            Absent = Convert.ToInt32(row["absent"]),
            Excused = Convert.ToInt32(row["excused"])
        });
    }

    public async Task<FacultyReportStatsDto> GetFacultyReportStatsAsync(int facId)
    {
        string query = @"
            -- ✅ CRITICAL FIX: Removed class_id filter - old records have NULL class_id
            WITH faculty_students AS (
                SELECT DISTINCT e.stud_id
                FROM enrollment e
                INNER JOIN classes c ON e.class_id = c.class_id
                WHERE c.fac_id = @facId
            ),
            -- Get today's attendance stats from daily_attendance_log
            today_stats AS (
                SELECT 
                    COUNT(DISTINCT CASE 
                        WHEN dal.time_in IS NOT NULL 
                        THEN dal.stud_id 
                    END) as present_count,
                    COUNT(DISTINCT CASE 
                        WHEN dal.time_in IS NOT NULL AND dal.time_out IS NULL 
                        THEN dal.stud_id 
                    END) as late_count,
                    COUNT(DISTINCT fs.stud_id) as total_enrolled
                FROM faculty_students fs
                LEFT JOIN daily_attendance_log dal 
                    ON fs.stud_id = dal.stud_id 
                    AND dal.attend_date = CURRENT_DATE
            ),
            -- Calculate overall attendance rate from last 30 days
            overall_stats AS (
                SELECT 
                    COUNT(DISTINCT CASE 
                        WHEN dal.time_in IS NOT NULL 
                        THEN CONCAT(dal.stud_id, '-', dal.attend_date) 
                    END) as attended_sessions,
                    COUNT(DISTINCT CONCAT(fs.stud_id, '-', dates.attend_date)) as total_sessions
                FROM faculty_students fs
                CROSS JOIN (
                    SELECT generate_series(
                        CURRENT_DATE - INTERVAL '30 days',
                        CURRENT_DATE,
                        INTERVAL '1 day'
                    )::date AS attend_date
                ) dates
                LEFT JOIN daily_attendance_log dal 
                    ON fs.stud_id = dal.stud_id 
                    AND dal.attend_date = dates.attend_date
            )
            SELECT 
                COALESCE(ROUND((CAST(os.attended_sessions AS DECIMAL) / NULLIF(os.total_sessions, 0)) * 100), 0) AS average_attendance,
                COALESCE(ts.late_count, 0) AS late_arrivals_today,
                COALESCE(ts.total_enrolled - ts.present_count, 0) AS absent_students,
                COALESCE(ts.total_enrolled, 0) AS total_students
            FROM today_stats ts
            CROSS JOIN overall_stats os;";

        var parameters = new Dictionary<string, object> { { "@facId", facId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);

        if (dt.Rows.Count == 0)
        {
            return new FacultyReportStatsDto
            {
                AverageAttendance = 0,
                LateArrivalsToday = 0,
                AbsentStudents = 0,
                TotalStudents = 0
            };
        }

        var row = dt.Rows[0];
        return new FacultyReportStatsDto
        {
            AverageAttendance = Convert.ToInt32(row["average_attendance"]),
            LateArrivalsToday = Convert.ToInt32(row["late_arrivals_today"]),
            AbsentStudents = Convert.ToInt32(row["absent_students"]),
            TotalStudents = Convert.ToInt32(row["total_students"])
        };
    }

    /// <summary>
    /// Gets complete student attendance data with statistics and recent records in a single optimized query
    /// Eliminates N+1 query problem by using JOINs and CTEs
    /// Returns both aggregated statistics and detailed attendance records
    /// ✅ FIXED: Now includes Time Out from daily_attendance_log table
    /// </summary>
    public async Task<StudentAttendanceDataDto> GetStudentAttendanceDataAsync(int studId, int limit = 20)
    {
        string query = @"
            -- Calculate attendance statistics based on class-level attendance
            WITH attendance_sessions AS (
                SELECT 
                    a.attend_date,
                    a.class_id,
                    a.attend_status
                FROM attendance a
                WHERE a.stud_id = @studId
            ),
            stats AS (
                SELECT 
                    COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as classes_attended,
                    COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late_arrivals,
                    COUNT(CASE WHEN attend_status = 'absent' THEN 1 END) as absences,
                    COUNT(*) as total_sessions
                FROM attendance_sessions
            ),
            -- ✅ FIXED: Get recent attendance records WITH Time In and Time Out from daily_attendance_log
            recent_attendance AS (
                SELECT DISTINCT ON (a.attend_date, a.class_id)
                    a.attend_date,
                    a.class_id,
                    COALESCE(co.course_code, 'N/A') as course_code,
                    a.attend_status::text as status,
                    dal.time_in,
                    dal.time_out,
                    a.attend_scan_time
                FROM attendance a
                INNER JOIN classes c ON a.class_id = c.class_id
                LEFT JOIN courses co ON c.course_id = co.course_id
                LEFT JOIN daily_attendance_log dal ON a.daily_attend_id = dal.daily_attend_id
                WHERE a.stud_id = @studId
                  AND a.attend_date >= CURRENT_DATE - INTERVAL '30 days'
                ORDER BY a.attend_date DESC, a.class_id, a.attend_scan_time DESC
            )
            -- Combine statistics and records
            SELECT 
                'STATS' as record_type,
                s.classes_attended,
                s.late_arrivals,
                s.absences,
                CASE 
                    WHEN s.total_sessions > 0 
                    THEN ROUND((CAST(s.classes_attended + s.late_arrivals AS DECIMAL) / s.total_sessions) * 100)
                    ELSE 0 
                END as attendance_rate,
                NULL::date as attend_date,
                NULL::text as course_code,
                NULL::text as status,
                NULL::timestamp as time_in,
                NULL::timestamp as time_out
            FROM stats s
            UNION ALL
            SELECT 
                'RECORD' as record_type,
                NULL::bigint as classes_attended,
                NULL::bigint as late_arrivals,
                NULL::bigint as absences,
                NULL::numeric as attendance_rate,
                ra.attend_date,
                ra.course_code,
                COALESCE(ra.status, 'pending') as status,
                ra.time_in,
                ra.time_out
            FROM recent_attendance ra
            ORDER BY record_type, attend_date DESC;";

        var parameters = new Dictionary<string, object> 
        { 
            { "@studId", studId },
            { "@limit", limit }
        };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        var result = new StudentAttendanceDataDto
        {
            Statistics = new StudentAttendanceStatsDto(),
            Records = new List<StudentAttendanceRecordDto>()
        };

        foreach (DataRow row in dt.Rows)
        {
            var recordType = row["record_type"]?.ToString();
            
            if (recordType == "STATS")
            {
                result.Statistics = new StudentAttendanceStatsDto
                {
                    ClassesAttended = row["classes_attended"] != DBNull.Value ? Convert.ToInt32(row["classes_attended"]) : 0,
                    LateArrivals = row["late_arrivals"] != DBNull.Value ? Convert.ToInt32(row["late_arrivals"]) : 0,
                    Absences = row["absences"] != DBNull.Value ? Convert.ToInt32(row["absences"]) : 0,
                    AttendanceRate = row["attendance_rate"] != DBNull.Value ? Convert.ToInt32(row["attendance_rate"]) : 0
                };
            }
            else if (recordType == "RECORD")
            {
                // ✅ FIXED: Format Time In and Time Out from daily_attendance_log
                string? timeInFormatted = null;
                string? timeOutFormatted = null;
                
                if (row["time_in"] != DBNull.Value)
                {
                    var timeInValue = Convert.ToDateTime(row["time_in"]);
                    timeInFormatted = timeInValue.ToString("hh:mm:ss tt");
                }
                
                if (row["time_out"] != DBNull.Value)
                {
                    var timeOutValue = Convert.ToDateTime(row["time_out"]);
                    timeOutFormatted = timeOutValue.ToString("hh:mm:ss tt");
                }
                
                result.Records.Add(new StudentAttendanceRecordDto
                {
                    Date = row["attend_date"] != DBNull.Value 
                        ? Convert.ToDateTime(row["attend_date"]).ToString("yyyy-MM-dd") 
                        : string.Empty,
                    CourseCode = row["course_code"]?.ToString() ?? "N/A",
                    Status = row["status"]?.ToString() ?? "pending",
                    TimeIn = timeInFormatted ?? "-",
                    TimeOut = timeOutFormatted ?? "-"  // ✅ NOW INCLUDES TIME OUT!
                });
            }
        }

        return result;
    }
    
    // ============================================================
    // NEW METHODS FOR PHASE 4 - SCHEDULE INTEGRATION
    // ============================================================
    
    /// <summary>
    /// Get attendance record for a specific class, student, and date
    /// Used for checking if attendance already exists before creating duplicate
    /// </summary>
    public async Task<Attendance?> GetByClassStudentDateAsync(int classId, int studId, DateTime date)
    {
        string query = @"
            SELECT attend_id, class_id, stud_id, attend_date, attend_scan_time, 
                   attend_type, attend_status, attend_remark, attend_created_at,
                   daily_attend_id, is_present
            FROM attendance
            WHERE class_id = @classId 
              AND stud_id = @studId 
              AND attend_date = @date
            LIMIT 1;";
        
        var parameters = new Dictionary<string, object>
        {
            { "@classId", classId },
            { "@studId", studId },
            { "@date", date.Date }
        };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Attendance
        {
            AttendId = Convert.ToInt32(row["attend_id"]),
            ClassId = row["class_id"] != DBNull.Value ? Convert.ToInt32(row["class_id"]) : null,
            StudId = row["stud_id"] != DBNull.Value ? Convert.ToInt32(row["stud_id"]) : null,
            AttendDate = Convert.ToDateTime(row["attend_date"]),
            AttendScanTime = Convert.ToDateTime(row["attend_scan_time"]),
            AttendType = Enum.Parse<ScanType>(row["attend_type"].ToString() ?? "In", true),
            AttendStatus = Enum.Parse<AttendanceStatus>(row["attend_status"].ToString() ?? "Pending", true),
            AttendRemark = row["attend_remark"]?.ToString(),
            AttendCreatedAt = Convert.ToDateTime(row["attend_created_at"]),
            DailyAttendId = row["daily_attend_id"] != DBNull.Value ? Convert.ToInt32(row["daily_attend_id"]) : null,
            IsPresent = row["is_present"] != DBNull.Value && Convert.ToBoolean(row["is_present"])
        };
    }

    /// <summary>
    /// ✅ NEW: Sync attendance record to attendance table for faculty tracking
    /// Creates or updates attendance record based on daily_attendance_log entry
    /// </summary>
    public async Task<bool> SyncAttendanceFromDailyLogAsync(int dailyAttendId, int classId, int studId, DateTime attendDate, DateTime scanTime, ScanType scanType)
    {
        // Check if record exists
        var existing = await GetByClassStudentDateAsync(classId, studId, attendDate);
        
        if (existing == null)
        {
            // Create new attendance record
            var newAttendance = new Attendance
            {
                ClassId = classId,
                StudId = studId,
                AttendDate = attendDate,
                AttendScanTime = scanTime,
                AttendType = scanType,
                AttendStatus = AttendanceStatus.Present, // Default to present since they scanned
                AttendRemark = "Auto-synced from daily attendance",
                AttendCreatedAt = DateTime.Now,
                DailyAttendId = dailyAttendId,
                IsPresent = true
            };
            
            return await AddAsync(newAttendance);
        }
        else
        {
            // Update existing record
            existing.AttendScanTime = scanTime;
            existing.AttendType = scanType;
            existing.DailyAttendId = dailyAttendId;
            existing.IsPresent = true;
            
            if (scanType == ScanType.Out)
            {
                existing.AttendStatus = AttendanceStatus.Present; // Completed attendance
            }
            
            return await UpdateAsync(existing);
        }
    }
}
