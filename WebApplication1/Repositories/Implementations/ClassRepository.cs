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
        string query = @"
            SELECT 
                c.class_id,
                COALESCE(co.course_code, 'N/A') as course_code,
                COALESCE(co.course_name, 'N/A') as course_name,
                COALESCE(r.room_code, 'N/A') as room_code,
                COUNT(DISTINCT e.stud_id) as student_count,
                COALESCE(
                    STRING_AGG(
                        TRIM(cs.sched_day_of_week) || ' ' || 
                        TO_CHAR(cs.sched_start_time, 'HH12:MI AM') || '-' || 
                        TO_CHAR(cs.sched_end_time, 'HH12:MI AM'), 
                        ', ' ORDER BY 
                            CASE TRIM(cs.sched_day_of_week)
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
            FROM classes c
            LEFT JOIN courses co ON c.course_id = co.course_id
            LEFT JOIN rooms r ON c.room_id = r.room_id
            LEFT JOIN class_schedules cs ON c.class_id = cs.class_id
            LEFT JOIN enrollment e ON c.class_id = e.class_id
            WHERE c.fac_id = @facId
            GROUP BY c.class_id, co.course_code, co.course_name, r.room_code
            ORDER BY co.course_code;";

        var parameters = new Dictionary<string, object> { { "@facId", facId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new FacultyClassDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseCode = row["course_code"]?.ToString() ?? "N/A",
            CourseName = row["course_name"]?.ToString() ?? "N/A",
            RoomCode = row["room_code"]?.ToString() ?? "N/A",
            StudentCount = Convert.ToInt32(row["student_count"]),
            Schedule = row["schedule"]?.ToString() ?? "TBA"
        });
    }

    /// <summary>
    /// ✅ NEW: Gets individual class schedules for conflict detection
    /// Returns separate rows for each day/time the class meets
    /// </summary>
    public async Task<IEnumerable<ClassScheduleDto>> GetClassSchedulesAsync(int classId)
    {
        string query = @"
            SELECT 
                sched_id,
                TRIM(sched_day_of_week) as day_of_week,
                TO_CHAR(sched_start_time, 'HH24:MI:SS') as start_time,
                TO_CHAR(sched_end_time, 'HH24:MI:SS') as end_time
            FROM class_schedules
            WHERE class_id = @classId
              AND TRIM(sched_day_of_week) != ''
            ORDER BY 
                CASE TRIM(sched_day_of_week)
                    WHEN 'Monday' THEN 1
                    WHEN 'Tuesday' THEN 2
                    WHEN 'Wednesday' THEN 3
                    WHEN 'Thursday' THEN 4
                    WHEN 'Friday' THEN 5
                    WHEN 'Saturday' THEN 6
                    WHEN 'Sunday' THEN 7
                END,
                sched_start_time;";

        var parameters = new Dictionary<string, object> { { "@classId", classId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new ClassScheduleDto
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            DayOfWeek = row["day_of_week"]?.ToString() ?? "",
            StartTime = row["start_time"]?.ToString() ?? "",
            EndTime = row["end_time"]?.ToString() ?? ""
        });
    }

    /// <summary>
    /// ✅ NEW: Check for duplicate class (same course + faculty + term)
    /// A class is considered duplicate if the same professor teaches the same course in the same term.
    /// Different rooms don't make it unique - it's still the same class assignment.
    /// </summary>
    /// <param name="courseId">Course ID</param>
    /// <param name="facId">Faculty ID</param>
    /// <param name="termId">Academic Term ID</param>
    /// <param name="excludeClassId">Class ID to exclude (for update operations)</param>
    /// <returns>Existing duplicate class if found, null otherwise</returns>
    public async Task<Class?> GetDuplicateClassAsync(int? courseId, int? facId, int? termId, int? excludeClassId = null)
    {
        // Build dynamic query based on which fields are provided
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object>();

        // Course matching
        if (courseId.HasValue)
        {
            conditions.Add("course_id = @courseId");
            parameters.Add("@courseId", courseId.Value);
        }
        else
        {
            conditions.Add("course_id IS NULL");
        }

        // Faculty matching
        if (facId.HasValue)
        {
            conditions.Add("fac_id = @facId");
            parameters.Add("@facId", facId.Value);
        }
        else
        {
            conditions.Add("fac_id IS NULL");
        }

        // Term matching
        if (termId.HasValue)
        {
            conditions.Add("term_id = @termId");
            parameters.Add("@termId", termId.Value);
        }
        else
        {
            conditions.Add("term_id IS NULL");
        }

        // Exclude current class (for update operations)
        if (excludeClassId.HasValue)
        {
            conditions.Add("class_id != @excludeClassId");
            parameters.Add("@excludeClassId", excludeClassId.Value);
        }

        string query = $@"
            SELECT 
                class_id, course_id, room_id, fac_id, term_id, 
                class_created_at, class_updated_at
            FROM classes
            WHERE {string.Join(" AND ", conditions)}
            LIMIT 1;";

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

    /// <summary>
    /// ✅ NEW: Get enrolled students for a class (for faculty student management)
    /// </summary>
    public async Task<IEnumerable<EnrolledStudentDto>> GetEnrolledStudentsForClassAsync(int classId)
    {
        string query = @"
            SELECT 
                e.enroll_id,
                s.stud_id,
                COALESCE(NULLIF(TRIM(s.stud_number), ''), 'N/A') as stud_number,
                CASE 
                    WHEN TRIM(COALESCE(u.user_first_name, '')) = '' AND TRIM(COALESCE(u.user_last_name, '')) = '' 
                    THEN 'Unknown Student'
                    WHEN TRIM(COALESCE(u.user_first_name, '')) = '' 
                    THEN TRIM(u.user_last_name)
                    WHEN TRIM(COALESCE(u.user_last_name, '')) = '' 
                    THEN TRIM(u.user_first_name)
                    ELSE TRIM(u.user_first_name) || ' ' || TRIM(u.user_last_name)
                END as full_name,
                COALESCE(NULLIF(TRIM(u.user_email), ''), 'N/A') as user_email,
                COALESCE(NULLIF(TRIM(p.program_name), ''), 'N/A') as program_name,
                COALESCE(s.stud_year_level, 0) as year_level,
                e.enroll_at,
                COALESCE(NULLIF(TRIM(u.user_profile_image), ''), 'templates/default_profile.png') as profile_image
            FROM enrollment e
            INNER JOIN students s ON e.stud_id = s.stud_id
            INNER JOIN users u ON s.user_id = u.user_id
            LEFT JOIN programs p ON s.program_id = p.program_id
            WHERE e.class_id = @classId
            ORDER BY u.user_last_name, u.user_first_name;";

        var parameters = new Dictionary<string, object> { { "@classId", classId } };
        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new EnrolledStudentDto
        {
            EnrollId = Convert.ToInt32(row["enroll_id"]),
            StudId = Convert.ToInt32(row["stud_id"]),
            StudNumber = row["stud_number"]?.ToString() ?? "N/A",
            FullName = row["full_name"]?.ToString() ?? "Unknown Student",
            Email = row["user_email"]?.ToString() ?? "N/A",
            ProgramName = row["program_name"]?.ToString() ?? "N/A",
            YearLevel = row["year_level"] != DBNull.Value ? Convert.ToInt32(row["year_level"]) : 0,
            EnrolledAt = row["enroll_at"] != DBNull.Value ? Convert.ToDateTime(row["enroll_at"]) : DateTime.MinValue,
            ProfileImage = row["profile_image"]?.ToString() ?? "templates/default_profile.png"
        });
    }
}
