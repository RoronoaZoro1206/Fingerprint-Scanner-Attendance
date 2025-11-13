using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class ClassScheduleRepository : IClassScheduleRepository
{
    private readonly DatabaseHelper _db;

    public ClassScheduleRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ClassSchedule>> GetAllAsync()
    {
        string query = @"SELECT sched_id, class_id, sched_day_of_week, sched_start_time, sched_end_time 
                         FROM class_schedules;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new ClassSchedule
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            SchedDayOfWeek = row["sched_day_of_week"].ToString() ?? "",
            SchedStartTime = (TimeSpan)row["sched_start_time"],
            SchedEndTime = (TimeSpan)row["sched_end_time"]
        });
    }

    public async Task<ClassSchedule?> GetByIdAsync(int schedId)
    {
        string query = @"SELECT sched_id, class_id, sched_day_of_week, sched_start_time, sched_end_time 
                         FROM class_schedules WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", schedId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new ClassSchedule
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            SchedDayOfWeek = row["sched_day_of_week"].ToString() ?? "",
            SchedStartTime = (TimeSpan)row["sched_start_time"],
            SchedEndTime = (TimeSpan)row["sched_end_time"]
        };
    }

    public async Task<IEnumerable<ClassSchedule>> GetByClassIdAsync(int classId)
    {
        string query = @"SELECT sched_id, class_id, sched_day_of_week, sched_start_time, sched_end_time 
                         FROM class_schedules WHERE class_id = @classId 
                         ORDER BY sched_day_of_week, sched_start_time;";
        var parameters = new Dictionary<string, object> { { "@classId", classId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new ClassSchedule
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            SchedDayOfWeek = row["sched_day_of_week"].ToString() ?? "",
            SchedStartTime = (TimeSpan)row["sched_start_time"],
            SchedEndTime = (TimeSpan)row["sched_end_time"]
        });
    }

    public async Task<bool> AddAsync(ClassSchedule sched)
    {
        string query = @"INSERT INTO class_schedules (class_id, sched_day_of_week, sched_start_time, sched_end_time)
                         VALUES (@class, @day, @start, @end);";
        var parameters = new Dictionary<string, object>
        {
            { "@class", sched.ClassId },
            { "@day", sched.SchedDayOfWeek },
            { "@start", sched.SchedStartTime },
            { "@end", sched.SchedEndTime }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(ClassSchedule sched)
    {
        string query = @"UPDATE class_schedules 
                         SET class_id = @class, sched_day_of_week = @day, 
                             sched_start_time = @start, sched_end_time = @end 
                         WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", sched.SchedId },
            { "@class", sched.ClassId },
            { "@day", sched.SchedDayOfWeek },
            { "@start", sched.SchedStartTime },
            { "@end", sched.SchedEndTime }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int schedId)
    {
        string query = "DELETE FROM class_schedules WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", schedId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int schedId)
    {
        string query = "SELECT COUNT(*) FROM class_schedules WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", schedId } };
        return await _db.RecordExistsAsync(query, parameters);
    }

    /// <summary>
    /// Gets all class schedules with course information in a single optimized query
    /// Eliminates N+1 query problem by using JOINs instead of multiple repository calls
    /// </summary>
    public async Task<IEnumerable<ClassScheduleDetailDto>> GetAllWithDetailsAsync()
    {
        string query = @"
            SELECT 
                cs.sched_id,
                cs.class_id,
                COALESCE(co.course_name, 'N/A') as course_name,
                cs.sched_day_of_week,
                TO_CHAR(cs.sched_start_time, 'HH24:MI') as sched_start_time,
                TO_CHAR(cs.sched_end_time, 'HH24:MI') as sched_end_time
            FROM class_schedules cs
            INNER JOIN classes cl ON cs.class_id = cl.class_id
            LEFT JOIN courses co ON cl.course_id = co.course_id
            ORDER BY cs.sched_id;";
        
        var dt = await _db.ExecuteQueryAsync(query);
        
        return dt.AsEnumerable().Select(row => new ClassScheduleDetailDto
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseName = row["course_name"]?.ToString() ?? "N/A",
            SchedDayOfWeek = row["sched_day_of_week"]?.ToString() ?? "",
            SchedStartTime = row["sched_start_time"]?.ToString() ?? "",
            SchedEndTime = row["sched_end_time"]?.ToString() ?? ""
        });
    }

    // ============================================================
    // NEW METHODS FOR SCHEDULE INTEGRATION - PHASE 4
    // ============================================================

    /// <summary>
    /// Get student's class schedule for a specific date
    /// Includes all class details, faculty info, and time slots
    /// Used for late detection when student times in
    /// </summary>
    public async Task<IEnumerable<StudentScheduleDto>> GetStudentScheduleForDateAsync(int studId, DateTime date)
    {
        // Get day of week (Monday, Tuesday, etc.)
        string dayOfWeek = date.DayOfWeek.ToString();
        
        string query = @"
            SELECT 
                c.class_id,
                co.course_id,
                co.course_code,
                co.course_name,
                co.course_units,
                cs.sched_day_of_week,
                cs.sched_start_time,
                cs.sched_end_time,
                COALESCE(r.room_code, 'TBA') as room_code,
                COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'TBA') as faculty_name
            FROM enrollment e
            INNER JOIN classes c ON e.class_id = c.class_id
            INNER JOIN courses co ON c.course_id = co.course_id
            INNER JOIN class_schedules cs ON c.class_id = cs.class_id
            LEFT JOIN rooms r ON c.room_id = r.room_id
            LEFT JOIN faculty f ON c.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            WHERE e.stud_id = @studId
              AND cs.sched_day_of_week = @dayOfWeek
              AND c.term_id = (SELECT term_id FROM academic_terms WHERE is_active = TRUE LIMIT 1)
            ORDER BY cs.sched_start_time;";
        
        var parameters = new Dictionary<string, object>
        {
            { "@studId", studId },
            { "@dayOfWeek", dayOfWeek }
        };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new StudentScheduleDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseId = Convert.ToInt32(row["course_id"]),
            CourseCode = row["course_code"].ToString() ?? "",
            CourseName = row["course_name"].ToString() ?? "",
            CourseUnits = Convert.ToInt32(row["course_units"]),
            DayOfWeek = row["sched_day_of_week"].ToString() ?? "",
            StartTime = (TimeSpan)row["sched_start_time"],
            EndTime = (TimeSpan)row["sched_end_time"],
            RoomCode = row["room_code"]?.ToString() ?? "TBA",
            FacultyName = row["faculty_name"]?.ToString() ?? "TBA"
        });
    }

    /// <summary>
    /// Get student's schedule for a specific day of week (e.g., "Monday")
    /// Helper method that calls GetStudentScheduleForDateAsync
    /// </summary>
    public async Task<IEnumerable<StudentScheduleDto>> GetStudentScheduleForDayAsync(int studId, string dayOfWeek)
    {
        // Calculate the next occurrence of this day of week
        var targetDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayOfWeek);
        var today = DateTime.Today;
        var daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;
        var targetDate = today.AddDays(daysUntilTarget);
        
        return await GetStudentScheduleForDateAsync(studId, targetDate);
    }
}
