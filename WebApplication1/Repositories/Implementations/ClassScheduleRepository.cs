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
}
