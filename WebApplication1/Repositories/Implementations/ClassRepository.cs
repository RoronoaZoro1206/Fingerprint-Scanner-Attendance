using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
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

    public async Task<IEnumerable<Class>> GetClassesByFacultyIdAsync(int facId)
    {
        string query = @"
            SELECT 
                c.class_id, c.course_id, c.room_id, c.fac_id, c.term_id,
                c.class_created_at, c.class_updated_at,
                co.course_code,
                co.course_name,
                r.room_code as room_name,
                t.term_name,
                (SELECT COUNT(*) FROM enrollment e WHERE e.class_id = c.class_id) as student_count
            FROM classes c
            LEFT JOIN courses co ON c.course_id = co.course_id
            LEFT JOIN rooms r ON c.room_id = r.room_id
            LEFT JOIN academic_terms t ON c.term_id = t.term_id
            WHERE c.fac_id = @facId
            ORDER BY c.class_id;";

        var parameters = new Dictionary<string, object> { { "@facId", facId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);

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
                CourseCode = row["course_code"]?.ToString() ?? "",
                CourseName = row["course_name"].ToString() ?? "N/A" 
            },
            Room = row["room_name"] == DBNull.Value ? null : new Room 
            { 
                RoomCode = row["room_name"].ToString() ?? "N/A" 
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

    // Optimized DTO methods
    public async Task<IEnumerable<ClassListDto>> GetAllClassListAsync()
    {
        string query = @"
            SELECT 
                c.class_id,
                c.course_id,
                c.room_id,
                c.fac_id,
                c.term_id,
                c.class_created_at,
                c.class_updated_at,
                COALESCE(co.course_name, 'N/A') AS course_name,
                COALESCE(r.room_code, 'N/A') AS room_name,
                COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'N/A') AS faculty_name,
                COALESCE(t.term_name, 'N/A') AS term_name
            FROM classes c
            LEFT JOIN courses co ON c.course_id = co.course_id
            LEFT JOIN rooms r ON c.room_id = r.room_id
            LEFT JOIN faculty f ON c.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            LEFT JOIN academic_terms t ON c.term_id = t.term_id
            ORDER BY c.class_id;";

        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new ClassListDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseId = row["course_id"] != DBNull.Value ? Convert.ToInt32(row["course_id"]) : null,
            CourseName = row["course_name"]?.ToString() ?? "N/A",
            RoomId = row["room_id"] != DBNull.Value ? Convert.ToInt32(row["room_id"]) : null,
            RoomName = row["room_name"]?.ToString() ?? "N/A",
            FacId = row["fac_id"] != DBNull.Value ? Convert.ToInt32(row["fac_id"]) : null,
            FacultyName = row["faculty_name"]?.ToString() ?? "N/A",
            TermId = row["term_id"] != DBNull.Value ? Convert.ToInt32(row["term_id"]) : null,
            TermName = row["term_name"]?.ToString() ?? "N/A",
            ClassCreatedAt = Convert.ToDateTime(row["class_created_at"]),
            ClassUpdatedAt = Convert.ToDateTime(row["class_updated_at"])
        });
    }

    public async Task<ClassDetailDto?> GetClassDetailByIdAsync(int classId)
    {
        string query = @"
            SELECT 
                c.class_id,
                c.course_id,
                c.room_id,
                c.fac_id,
                c.term_id,
                c.class_created_at,
                c.class_updated_at,
                co.course_code,
                co.course_name,
                r.room_code,
                r.room_description,
                CONCAT(u.user_first_name, ' ', u.user_last_name) AS faculty_name,
                t.term_name,
                t.start_date,
                t.end_date
            FROM classes c
            LEFT JOIN courses co ON c.course_id = co.course_id
            LEFT JOIN rooms r ON c.room_id = r.room_id
            LEFT JOIN faculty f ON c.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            LEFT JOIN academic_terms t ON c.term_id = t.term_id
            WHERE c.class_id = @id
            LIMIT 1;";

        var parameters = new Dictionary<string, object> { { "@id", classId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);

        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new ClassDetailDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseId = row["course_id"] != DBNull.Value ? Convert.ToInt32(row["course_id"]) : null,
            CourseCode = row["course_code"]?.ToString() ?? "",
            CourseName = row["course_name"]?.ToString() ?? "",
            RoomId = row["room_id"] != DBNull.Value ? Convert.ToInt32(row["room_id"]) : null,
            RoomCode = row["room_code"]?.ToString() ?? "",
            RoomDescription = row["room_description"]?.ToString(),
            FacId = row["fac_id"] != DBNull.Value ? Convert.ToInt32(row["fac_id"]) : null,
            FacultyName = row["faculty_name"]?.ToString() ?? "",
            TermId = row["term_id"] != DBNull.Value ? Convert.ToInt32(row["term_id"]) : null,
            TermName = row["term_name"]?.ToString() ?? "",
            TermStartDate = row["start_date"] != DBNull.Value ? Convert.ToDateTime(row["start_date"]) : null,
            TermEndDate = row["end_date"] != DBNull.Value ? Convert.ToDateTime(row["end_date"]) : null,
            ClassCreatedAt = Convert.ToDateTime(row["class_created_at"]),
            ClassUpdatedAt = Convert.ToDateTime(row["class_updated_at"])
        };
    }

    public async Task<IEnumerable<FacultyClassDto>> GetFacultyClassesAsync(int facId)
    {
        // ✅ FIX: Changed from MIN(sched_id) subquery to STRING_AGG to include ALL schedules
        string query = @"
            SELECT 
                c.class_id,
                COALESCE(co.course_code, 'N/A') AS course_code,
                COALESCE(co.course_name, 'N/A') AS course_name,
                COALESCE(r.room_code, 'N/A') AS room_name,
                COALESCE(t.term_name, 'N/A') AS term_name,
                COALESCE(
                    STRING_AGG(
                        cs.sched_day_of_week || ' ' || 
                        TO_CHAR(cs.sched_start_time, 'HH12:MI') || ' - ' || 
                        TO_CHAR(cs.sched_end_time, 'HH12:MI'), 
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
                    (SELECT COUNT(*) FROM enrollment e WHERE e.class_id = c.class_id),
                    0
                ) AS student_count
            FROM classes c
            LEFT JOIN courses co ON c.course_id = co.course_id
            LEFT JOIN rooms r ON c.room_id = r.room_id
            LEFT JOIN academic_terms t ON c.term_id = t.term_id
            LEFT JOIN class_schedules cs ON c.class_id = cs.class_id
            WHERE c.fac_id = @facId
            GROUP BY c.class_id, co.course_code, co.course_name, r.room_code, t.term_name
            ORDER BY c.class_id;";

        var parameters = new Dictionary<string, object> { { "@facId", facId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new FacultyClassDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseCode = row["course_code"]?.ToString() ?? "N/A",
            CourseName = row["course_name"]?.ToString() ?? "N/A",
            RoomName = row["room_name"]?.ToString() ?? "N/A",
            Schedule = row["schedule"]?.ToString() ?? "",
            StudentCount = Convert.ToInt32(row["student_count"]),
            TermName = row["term_name"]?.ToString() ?? "N/A"
        });
    }
}
