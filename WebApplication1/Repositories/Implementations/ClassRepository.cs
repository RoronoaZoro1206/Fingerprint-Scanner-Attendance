using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class ClassRepository : IClassRepository
{
    private readonly DatabaseHelper _db;

    public ClassRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Class>> GetAllAsync()
    {
        string query = @"SELECT class_id, course_id, room_id, fac_id, class_created_at, class_updated_at 
                         FROM classes;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Class
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseId = row["course_id"] == DBNull.Value ? null : Convert.ToInt32(row["course_id"]),
            RoomId = row["room_id"] == DBNull.Value ? null : Convert.ToInt32(row["room_id"]),
            FacId = row["fac_id"] == DBNull.Value ? null : Convert.ToInt32(row["fac_id"]),
            ClassCreatedAt = Convert.ToDateTime(row["class_created_at"]),
            ClassUpdatedAt = Convert.ToDateTime(row["class_updated_at"])
        });
    }

    public async Task<Class?> GetByIdAsync(int classId)
    {
        string query = @"SELECT class_id, course_id, room_id, fac_id, class_created_at, class_updated_at 
                         FROM classes WHERE class_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", classId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Class
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseId = row["course_id"] == DBNull.Value ? null : Convert.ToInt32(row["course_id"]),
            RoomId = row["room_id"] == DBNull.Value ? null : Convert.ToInt32(row["room_id"]),
            FacId = row["fac_id"] == DBNull.Value ? null : Convert.ToInt32(row["fac_id"]),
            ClassCreatedAt = Convert.ToDateTime(row["class_created_at"]),
            ClassUpdatedAt = Convert.ToDateTime(row["class_updated_at"])
        };
    }

    public async Task<bool> AddAsync(Class cls)
    {
        string query = @"INSERT INTO classes (course_id, room_id, fac_id, class_created_at, class_updated_at)
                         VALUES (@course, @room, @fac, @created, @updated);";
        var parameters = new Dictionary<string, object>
        {
            { "@course", cls.CourseId ?? (object)DBNull.Value },
            { "@room", cls.RoomId ?? (object)DBNull.Value },
            { "@fac", cls.FacId ?? (object)DBNull.Value },
            { "@created", cls.ClassCreatedAt },
            { "@updated", cls.ClassUpdatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Class cls)
    {
        string query = @"UPDATE classes 
                         SET course_id = @course, room_id = @room, fac_id = @fac, 
                             class_created_at = @created, class_updated_at = @updated 
                         WHERE class_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", cls.ClassId },
            { "@course", cls.CourseId ?? (object)DBNull.Value },
            { "@room", cls.RoomId ?? (object)DBNull.Value },
            { "@fac", cls.FacId ?? (object)DBNull.Value },
            { "@created", cls.ClassCreatedAt },
            { "@updated", cls.ClassUpdatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int classId)
    {
        string query = "DELETE FROM classes WHERE class_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", classId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int classId)
    {
        string query = "SELECT COUNT(*) FROM classes WHERE class_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", classId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
