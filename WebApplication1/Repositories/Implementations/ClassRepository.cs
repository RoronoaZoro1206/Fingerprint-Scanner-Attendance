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
        string query = @"
            SELECT 
                class_id, course_id, room_id, fac_id, term_id, 
                class_created_at, class_updated_at
            FROM classes
            ORDER BY class_id;";

        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Class
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseId = row["course_id"] == DBNull.Value ? null : Convert.ToInt32(row["course_id"]),
            RoomId = row["room_id"] == DBNull.Value ? null : Convert.ToInt32(row["room_id"]),
            FacId = row["fac_id"] == DBNull.Value ? null : Convert.ToInt32(row["fac_id"]),
            TermId = row["term_id"] == DBNull.Value ? null : Convert.ToInt32(row["term_id"]),
            ClassCreatedAt = Convert.ToDateTime(row["class_created_at"]),
            ClassUpdatedAt = Convert.ToDateTime(row["class_updated_at"])
        });
    }

    public async Task<IEnumerable<Class>> GetAllWithDetailsAsync()
    {
        string query = @"
            SELECT 
                c.class_id, c.course_id, c.room_id, c.fac_id, c.term_id,
                c.class_created_at, c.class_updated_at,
                co.course_name,
                r.room_code as room_name,
                CONCAT(u.user_first_name, ' ', u.user_last_name) as faculty_name,
                t.term_name
            FROM classes c
            LEFT JOIN courses co ON c.course_id = co.course_id
            LEFT JOIN rooms r ON c.room_id = r.room_id
            LEFT JOIN faculty f ON c.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            LEFT JOIN academic_terms t ON c.term_id = t.term_id
            ORDER BY c.class_id;";

        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Class
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseId = row["course_id"] == DBNull.Value ? null : Convert.ToInt32(row["course_id"]),
            RoomId = row["room_id"] == DBNull.Value ? null : Convert.ToInt32(row["room_id"]),
            FacId = row["fac_id"] == DBNull.Value ? null : Convert.ToInt32(row["fac_id"]),
            TermId = row["term_id"] == DBNull.Value ? null : Convert.ToInt32(row["term_id"]),
            ClassCreatedAt = Convert.ToDateTime(row["class_created_at"]),
            ClassUpdatedAt = Convert.ToDateTime(row["class_updated_at"]),
            Course = row["course_name"] == DBNull.Value ? null : new Course 
            { 
                CourseName = row["course_name"].ToString() ?? "N/A" 
            },
            Room = row["room_name"] == DBNull.Value ? null : new Room 
            { 
                RoomCode = row["room_name"].ToString() ?? "N/A" 
            },
            Faculty = row["faculty_name"] == DBNull.Value ? null : new Faculty 
            { 
                User = new User
                {
                    UserFirstName = row["faculty_name"].ToString()?.Split(' ').FirstOrDefault() ?? "N/A",
                    UserLastName = row["faculty_name"].ToString()?.Split(' ').Skip(1).FirstOrDefault() ?? ""
                }
            },
            Term = row["term_name"] == DBNull.Value ? null : new AcademicTerm 
            { 
                TermName = row["term_name"].ToString() ?? "N/A" 
            }
        });
    }

    public async Task<Class?> GetByIdAsync(int classId)
    {
        string query = @"
            SELECT 
                class_id, course_id, room_id, fac_id, term_id, 
                class_created_at, class_updated_at
            FROM classes
            WHERE class_id = @id;";

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
            TermId = row["term_id"] == DBNull.Value ? null : Convert.ToInt32(row["term_id"]),
            ClassCreatedAt = Convert.ToDateTime(row["class_created_at"]),
            ClassUpdatedAt = Convert.ToDateTime(row["class_updated_at"])
        };
    }

    public async Task<bool> AddAsync(Class cls)
    {
        string query = @"
            INSERT INTO classes 
                (course_id, room_id, fac_id, term_id, class_created_at, class_updated_at)
            VALUES 
                (@course, @room, @fac, @term, @created, @updated);";

        var parameters = new Dictionary<string, object>
        {
            { "@course", cls.CourseId ?? (object)DBNull.Value },
            { "@room", cls.RoomId ?? (object)DBNull.Value },
            { "@fac", cls.FacId ?? (object)DBNull.Value },
            { "@term", cls.TermId ?? (object)DBNull.Value },
            { "@created", cls.ClassCreatedAt },
            { "@updated", cls.ClassUpdatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Class cls)
    {
        string query = @"
            UPDATE classes SET
                course_id = @course,
                room_id = @room,
                fac_id = @fac,
                term_id = @term,
                class_updated_at = @updated
            WHERE class_id = @id;";

        var parameters = new Dictionary<string, object>
        {
            { "@id", cls.ClassId },
            { "@course", cls.CourseId ?? (object)DBNull.Value },
            { "@room", cls.RoomId ?? (object)DBNull.Value },
            { "@fac", cls.FacId ?? (object)DBNull.Value },
            { "@term", cls.TermId ?? (object)DBNull.Value },
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
