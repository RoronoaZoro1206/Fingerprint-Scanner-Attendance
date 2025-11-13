using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly DatabaseHelper _db;

    public EnrollmentRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Enrollment>> GetAllAsync()
    {
        string query = @"SELECT enroll_id, stud_id, class_id, enroll_at 
                         FROM enrollment;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Enrollment
        {
            EnrollId = Convert.ToInt32(row["enroll_id"]),
            StudId = Convert.ToInt32(row["stud_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            EnrollAt = Convert.ToDateTime(row["enroll_at"])
        });
    }

    public async Task<Enrollment?> GetByIdAsync(int enrollId)
    {
        string query = @"SELECT enroll_id, stud_id, class_id, enroll_at 
                         FROM enrollment WHERE enroll_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", enrollId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Enrollment
        {
            EnrollId = Convert.ToInt32(row["enroll_id"]),
            StudId = Convert.ToInt32(row["stud_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            EnrollAt = Convert.ToDateTime(row["enroll_at"])
        };
    }

    public async Task<IEnumerable<Enrollment>> GetByClassIdAsync(int classId)
    {
        string query = @"SELECT enroll_id, stud_id, class_id, enroll_at 
                         FROM enrollment WHERE class_id = @classId;";
        var parameters = new Dictionary<string, object> { { "@classId", classId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new Enrollment
        {
            EnrollId = Convert.ToInt32(row["enroll_id"]),
            StudId = Convert.ToInt32(row["stud_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            EnrollAt = Convert.ToDateTime(row["enroll_at"])
        });
    }

    public async Task<IEnumerable<Enrollment>> GetByStudentIdAsync(int studId)
    {
        string query = @"SELECT enroll_id, stud_id, class_id, enroll_at 
                         FROM enrollment WHERE stud_id = @studId;";
        var parameters = new Dictionary<string, object> { { "@studId", studId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new Enrollment
        {
            EnrollId = Convert.ToInt32(row["enroll_id"]),
            StudId = Convert.ToInt32(row["stud_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            EnrollAt = Convert.ToDateTime(row["enroll_at"])
        });
    }

    public async Task<bool> AddAsync(Enrollment enroll)
    {
        string query = @"INSERT INTO enrollment (stud_id, class_id, enroll_at)
                         VALUES (@stud, @class, @at);";
        var parameters = new Dictionary<string, object>
        {
            { "@stud", enroll.StudId },
            { "@class", enroll.ClassId },
            { "@at", enroll.EnrollAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Enrollment enroll)
    {
        string query = @"UPDATE enrollment 
                         SET stud_id = @stud, class_id = @class, enroll_at = @at 
                         WHERE enroll_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", enroll.EnrollId },
            { "@stud", enroll.StudId },
            { "@class", enroll.ClassId },
            { "@at", enroll.EnrollAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int enrollId)
    {
        string query = "DELETE FROM enrollment WHERE enroll_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", enrollId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int enrollId)
    {
        string query = "SELECT COUNT(*) FROM enrollment WHERE enroll_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", enrollId } };
        return await _db.RecordExistsAsync(query, parameters);
    }

    /// <summary>
    /// Gets student enrollments with all related details in a single optimized query
    /// Eliminates N+1 query problem by using JOINs instead of multiple repository calls
    /// ? FIXED: Added NULL value handling with COALESCE
    /// </summary>
    public async Task<IEnumerable<EnrollmentDetailDto>> GetStudentEnrollmentsWithDetailsAsync(int studId)
    {
        string query = @"
            SELECT 
                e.enroll_id,
                e.class_id,
                COALESCE(co.course_code, 'UNKNOWN') as course_code,
                COALESCE(co.course_name, 'Unknown Course') as course_name,
                COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'No Faculty Assigned') as faculty_name,
                COALESCE(r.room_code, 'TBA') as room_code,
                COALESCE(
                    STRING_AGG(
                        cs.sched_day_of_week || ' ' || 
                        TO_CHAR(cs.sched_start_time, 'HH12:MI AM') || '-' || 
                        TO_CHAR(cs.sched_end_time, 'HH12:MI AM'), 
                        ', ' ORDER BY 
                            CASE cs.sched_day_of_week
                                WHEN 'Monday' THEN 1
                                WHEN 'Tuesday' THEN 2
                                WHEN 'Wednesday' THEN 3
                                WHEN 'Thursday' THEN 4
                                WHEN 'Friday' THEN 5
                                WHEN 'Saturday' THEN 6
                                WHEN 'Sunday' THEN 7
                            END,
                            cs.sched_start_time
                    ),
                    'TBA'
                ) as schedule
            FROM enrollment e
            INNER JOIN classes cl ON e.class_id = cl.class_id
            LEFT JOIN courses co ON cl.course_id = co.course_id
            LEFT JOIN rooms r ON cl.room_id = r.room_id
            LEFT JOIN faculty f ON cl.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            LEFT JOIN class_schedules cs ON cl.class_id = cs.class_id
            WHERE e.stud_id = @studId
            GROUP BY e.enroll_id, e.class_id, co.course_code, co.course_name, 
                     u.user_first_name, u.user_last_name, r.room_code
            ORDER BY co.course_code;";
        
        var parameters = new Dictionary<string, object> { { "@studId", studId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);
        
        return dt.AsEnumerable().Select(row => new EnrollmentDetailDto
        {
            EnrollId = Convert.ToInt32(row["enroll_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseCode = row["course_code"]?.ToString() ?? "UNKNOWN",
            CourseName = row["course_name"]?.ToString() ?? "Unknown Course",
            FacultyName = row["faculty_name"]?.ToString() ?? "No Faculty Assigned",
            RoomCode = row["room_code"]?.ToString() ?? "TBA",
            Schedule = row["schedule"]?.ToString() ?? "TBA"
        });
    }

    /// <summary>
    /// Gets available classes for a student to enroll in with all details in a single query
    /// Filters out already enrolled classes and matches student's program
    /// ? FIXED: Added NULL value handling, active term filtering, and better error handling
    /// </summary>
    public async Task<IEnumerable<AvailableClassDto>> GetAvailableClassesForStudentAsync(int studId, int programId)
    {
        string query = @"
            SELECT 
                cl.class_id,
                COALESCE(co.course_code, 'UNKNOWN') as course_code,
                COALESCE(co.course_name, 'Unknown Course') as course_name,
                COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'No Faculty Assigned') as faculty_name,
                COALESCE(r.room_code, 'TBA') as room_code,
                COALESCE(
                    STRING_AGG(
                        cs.sched_day_of_week || ' ' || 
                        TO_CHAR(cs.sched_start_time, 'HH12:MI AM') || '-' || 
                        TO_CHAR(cs.sched_end_time, 'HH12:MI AM'), 
                        ', ' ORDER BY 
                            CASE cs.sched_day_of_WEEK
                                WHEN 'Monday' THEN 1
                                WHEN 'Tuesday' THEN 2
                                WHEN 'Wednesday' THEN 3
                                WHEN 'Thursday' THEN 4
                                WHEN 'Friday' THEN 5
                                WHEN 'Saturday' THEN 6
                                WHEN 'Sunday' THEN 7
                            END,
                            cs.sched_start_time
                    ), 
                    'TBA'
                ) as schedule
            FROM classes cl
            INNER JOIN courses co ON cl.course_id = co.course_id
            INNER JOIN academic_terms at ON cl.term_id = at.term_id
            LEFT JOIN rooms r ON cl.room_id = r.room_id
            LEFT JOIN faculty f ON cl.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            LEFT JOIN class_schedules cs ON cl.class_id = cs.class_id
            WHERE co.program_id = @programId
              AND at.is_active = TRUE
              AND cl.class_id NOT IN (
                  SELECT class_id FROM enrollment WHERE stud_id = @studId
              )
            GROUP BY cl.class_id, co.course_code, co.course_name, 
                     u.user_first_name, u.user_last_name, r.room_code
            ORDER BY co.course_code;";
        
        var parameters = new Dictionary<string, object> 
        { 
            { "@studId", studId },
            { "@programId", programId }
        };
        
        var dt = await _db.ExecuteQueryAsync(query, parameters);
        
        return dt.AsEnumerable().Select(row => new AvailableClassDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseCode = row["course_code"]?.ToString() ?? "UNKNOWN",
            CourseName = row["course_name"]?.ToString() ?? "Unknown Course",
            FacultyName = row["faculty_name"]?.ToString() ?? "No Faculty Assigned",
            RoomCode = row["room_code"]?.ToString() ?? "TBA",
            Schedule = row["schedule"]?.ToString() ?? "TBA"
        });
    }

    public async Task<IEnumerable<StudentClassDto>> GetStudentClassesAsync(int studId)
    {
        string query = @"
            SELECT 
                cl.class_id,
                COALESCE(co.course_code, 'N/A') AS course_code,
                COALESCE(co.course_name, 'N/A') AS course_name,
                COALESCE(co.course_units, 0) AS course_units,
                COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'N/A') AS faculty_name,
                COALESCE(r.room_code, 'N/A') AS room_code,
                COALESCE(
                    STRING_AGG(
                        cs.sched_day_of_week || ' ' || 
                        TO_CHAR(cs.sched_start_time, 'HH12:MI AM') || '-' || 
                        TO_CHAR(cs.sched_end_time, 'HH12:MI AM'), 
                        ', ' ORDER BY 
                            CASE cs.sched_day_of_week
                                WHEN 'Monday' THEN 1
                                WHEN 'Tuesday' THEN 2
                                WHEN 'Wednesday' THEN 3
                                WHEN 'Thursday' THEN 4
                                WHEN 'Friday' THEN 5
                                WHEN 'Saturday' THEN 6
                                WHEN 'Sunday' THEN 7
                            END,
                            cs.sched_start_time
                    ),
                    ''
                ) AS schedule,
                COALESCE(
                    ROUND(
                        (COUNT(CASE WHEN a.attend_status = 'present' THEN 1 END)::DECIMAL / 
                         NULLIF(COUNT(a.attend_id), 0)) * 100
                    ),
                    0
                ) AS attendance_rate
            FROM enrollment e
            INNER JOIN classes cl ON e.class_id = cl.class_id
            LEFT JOIN courses co ON cl.course_id = co.course_id
            LEFT JOIN rooms r ON cl.room_id = r.room_id
            LEFT JOIN faculty f ON cl.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            LEFT JOIN class_schedules cs ON cl.class_id = cs.class_id
            LEFT JOIN attendance a ON cl.class_id = a.class_id AND a.stud_id = e.stud_id
            WHERE e.stud_id = @studId
            GROUP BY cl.class_id, co.course_code, co.course_name, co.course_units,
                     u.user_first_name, u.user_last_name, r.room_code
            ORDER BY co.course_code;";

        var parameters = new Dictionary<string, object> { { "@studId", studId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new StudentClassDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseCode = row["course_code"]?.ToString() ?? "N/A",
            CourseName = row["course_name"]?.ToString() ?? "N/A",
            CourseUnits = Convert.ToInt32(row["course_units"]),
            FacultyName = row["faculty_name"]?.ToString() ?? "N/A",
            RoomCode = row["room_code"]?.ToString() ?? "N/A",
            Schedule = row["schedule"]?.ToString() ?? "",
            AttendanceRate = Convert.ToInt32(row["attendance_rate"])
        });
    }
}
