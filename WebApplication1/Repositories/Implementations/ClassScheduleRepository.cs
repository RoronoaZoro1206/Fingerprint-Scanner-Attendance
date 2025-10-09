using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
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
        string query = @"SELECT sched_id, class_id, sched_dayofweek, sched_starttime, sched_endtime 
                         FROM class_schedules;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new ClassSchedule
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            SchedDayOfWeek = row["sched_dayofweek"].ToString() ?? "",
            SchedStartTime = (TimeSpan)row["sched_starttime"],
            SchedEndTime = (TimeSpan)row["sched_endtime"]
        });
    }

    public async Task<ClassSchedule?> GetByIdAsync(int schedId)
    {
        string query = @"SELECT sched_id, class_id, sched_dayofweek, sched_starttime, sched_endtime 
                         FROM class_schedules WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", schedId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new ClassSchedule
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            SchedDayOfWeek = row["sched_dayofweek"].ToString() ?? "",
            SchedStartTime = (TimeSpan)row["sched_starttime"],
            SchedEndTime = (TimeSpan)row["sched_endtime"]
        };
    }

    public async Task<bool> AddAsync(ClassSchedule sched)
    {
        string query = @"INSERT INTO class_schedules (class_id, sched_dayofweek, sched_starttime, sched_endtime)
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
                         SET class_id = @class, sched_dayofweek = @day, 
                             sched_starttime = @start, sched_endtime = @end 
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
}
