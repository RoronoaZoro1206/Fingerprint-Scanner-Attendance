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
            -- Get all enrolled students with their attendance for the date
            student_attendance AS (
                SELECT 
                    s.stud_id,
                    s.stud_number,
                    CONCAT(u.user_first_name, ' ', u.user_last_name) as student_name,
                    COALESCE(u.user_profile_image, 'default_profile.png') as user_profile_image,
                    MIN(CASE WHEN a.attend_type = 'in' THEN a.attend_scan_time END) as time_in,
                    MIN(CASE WHEN a.attend_type = 'out' THEN a.attend_scan_time END) as time_out,
                    COALESCE(
                        (SELECT attend_status::text FROM attendance 
                         WHERE class_id = @classId AND stud_id = s.stud_id 
                           AND attend_date = @date 
                         ORDER BY attend_scan_time DESC LIMIT 1),
                        'absent'
                    ) as status,
                    (SELECT attend_id FROM attendance 
                     WHERE class_id = @classId AND stud_id = s.stud_id 
                       AND attend_date = @date AND attend_type = 'in' 
                     LIMIT 1) as attend_id
                FROM enrollment e
                INNER JOIN students s ON e.stud_id = s.stud_id
                INNER JOIN users u ON s.user_id = u.user_id
                LEFT JOIN attendance a ON e.stud_id = a.stud_id 
                    AND a.class_id = @classId 
                    AND a.attend_date = @date
                WHERE e.class_id = @classId
                GROUP BY s.stud_id, s.stud_number, u.user_first_name, 
                         u.user_last_name, u.user_profile_image
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
                sa.attend_id
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
}
