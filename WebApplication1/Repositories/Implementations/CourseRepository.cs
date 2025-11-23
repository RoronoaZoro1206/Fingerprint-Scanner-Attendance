using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;

public class CourseRepository : ICourseRepository
{
    private readonly DatabaseHelper _db;

    public CourseRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        string query = "SELECT * FROM courses ORDER BY course_id;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Course
        {
            CourseId = Convert.ToInt32(row["course_id"]),
            CourseCode = row["course_code"].ToString() ?? "",
            CourseName = row["course_name"].ToString() ?? "",
            CourseDesc = row["course_desc"]?.ToString(),
            CourseUnits = Convert.ToInt32(row["course_units"]),
            ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : null,
            CourseCreatedAt = Convert.ToDateTime(row["course_created_at"])
        });
    }

    public async Task<Course?> GetByIdAsync(int courseId)
    {
        string query = "SELECT * FROM courses WHERE course_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", courseId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Course
        {
            CourseId = Convert.ToInt32(row["course_id"]),
            CourseCode = row["course_code"].ToString() ?? "",
            CourseName = row["course_name"].ToString() ?? "",
            CourseDesc = row["course_desc"]?.ToString(),
            CourseUnits = Convert.ToInt32(row["course_units"]),
            ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : null,
            CourseCreatedAt = Convert.ToDateTime(row["course_created_at"])
        };
    }

    public async Task<bool> AddAsync(Course course)
    {
        string query = @"
            INSERT INTO courses (course_code, course_name, course_desc, course_units, program_id, course_created_at)
            VALUES (@code, @name, @desc, @units, @program, @created);";

        var parameters = new Dictionary<string, object>
        {
            { "@code", course.CourseCode },
            { "@name", course.CourseName },
            { "@desc", course.CourseDesc ?? (object)DBNull.Value },
            { "@units", course.CourseUnits },
            { "@program", course.ProgramId ?? (object)DBNull.Value },
            { "@created", course.CourseCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Course course)
    {
        string query = @"
            UPDATE courses
            SET course_code = @code,
                course_name = @name,
                course_desc = @desc,
                course_units = @units,
                program_id = @program
            WHERE course_id = @id;";

        var parameters = new Dictionary<string, object>
        {
            { "@id", course.CourseId },
            { "@code", course.CourseCode },
            { "@name", course.CourseName },
            { "@desc", course.CourseDesc ?? (object)DBNull.Value },
            { "@units", course.CourseUnits },
            { "@program", course.ProgramId ?? (object)DBNull.Value }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int courseId)
    {
        string query = "DELETE FROM courses WHERE course_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", courseId } };
        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    // ============================================================
    // ✅ NEW: DUPLICATE VALIDATION METHODS
    // ============================================================
    
    /// <summary>
    /// Get course by course code (case-insensitive)
    /// </summary>
    public async Task<Course?> GetByCourseCodeAsync(string courseCode)
    {
        string query = "SELECT * FROM courses WHERE LOWER(course_code) = LOWER(@code);";
        var parameters = new Dictionary<string, object> { { "@code", courseCode } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Course
        {
            CourseId = Convert.ToInt32(row["course_id"]),
            CourseCode = row["course_code"].ToString() ?? "",
            CourseName = row["course_name"].ToString() ?? "",
            CourseDesc = row["course_desc"]?.ToString(),
            CourseUnits = Convert.ToInt32(row["course_units"]),
            ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : null,
            CourseCreatedAt = Convert.ToDateTime(row["course_created_at"])
        };
    }

    /// <summary>
    /// Get course by course name (case-insensitive)
    /// </summary>
    public async Task<Course?> GetByCourseNameAsync(string courseName)
    {
        string query = "SELECT * FROM courses WHERE LOWER(course_name) = LOWER(@name);";
        var parameters = new Dictionary<string, object> { { "@name", courseName } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Course
        {
            CourseId = Convert.ToInt32(row["course_id"]),
            CourseCode = row["course_code"].ToString() ?? "",
            CourseName = row["course_name"].ToString() ?? "",
            CourseDesc = row["course_desc"]?.ToString(),
            CourseUnits = Convert.ToInt32(row["course_units"]),
            ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : null,
            CourseCreatedAt = Convert.ToDateTime(row["course_created_at"])
        };
    }

    /// <summary>
    /// Check if course code is duplicate (case-insensitive)
    /// </summary>
    /// <param name="courseCode">Course code to check</param>
    /// <param name="excludeCourseId">Course ID to exclude (for updates)</param>
    /// <returns>True if duplicate exists, false otherwise</returns>
    public async Task<bool> IsCourseCodeDuplicateAsync(string courseCode, int? excludeCourseId = null)
    {
        string query = excludeCourseId.HasValue
            ? "SELECT COUNT(*) FROM courses WHERE LOWER(course_code) = LOWER(@code) AND course_id != @excludeId;"
            : "SELECT COUNT(*) FROM courses WHERE LOWER(course_code) = LOWER(@code);";

        var parameters = new Dictionary<string, object> { { "@code", courseCode } };
        if (excludeCourseId.HasValue)
        {
            parameters.Add("@excludeId", excludeCourseId.Value);
        }

        return await _db.RecordExistsAsync(query, parameters);
    }

    /// <summary>
    /// Check if course name is duplicate (case-insensitive)
    /// </summary>
    /// <param name="courseName">Course name to check</param>
    /// <param name="excludeCourseId">Course ID to exclude (for updates)</param>
    /// <returns>True if duplicate exists, false otherwise</returns>
    public async Task<bool> IsCourseNameDuplicateAsync(string courseName, int? excludeCourseId = null)
    {
        string query = excludeCourseId.HasValue
            ? "SELECT COUNT(*) FROM courses WHERE LOWER(course_name) = LOWER(@name) AND course_id != @excludeId;"
            : "SELECT COUNT(*) FROM courses WHERE LOWER(course_name) = LOWER(@name);";

        var parameters = new Dictionary<string, object> { { "@name", courseName } };
        if (excludeCourseId.HasValue)
        {
            parameters.Add("@excludeId", excludeCourseId.Value);
        }

        return await _db.RecordExistsAsync(query, parameters);
    }

    /// <summary>
    /// Gets all courses with program information in a single optimized query
    /// Eliminates N+1 query problem by using JOIN instead of multiple repository calls
    /// </summary>
    public async Task<IEnumerable<CourseDetailDto>> GetAllWithProgramAsync()
    {
        string query = @"
            SELECT 
                c.course_id,
                c.course_code,
                c.course_name,
                c.course_desc,
                c.course_units,
                c.program_id,
                COALESCE(p.program_name, 'N/A') as program_name
            FROM courses c
            LEFT JOIN programs p ON c.program_id = p.program_id
            ORDER BY c.course_id;";
        
        var dt = await _db.ExecuteQueryAsync(query);
        
        return dt.AsEnumerable().Select(row => new CourseDetailDto
        {
            CourseId = Convert.ToInt32(row["course_id"]),
            CourseCode = row["course_code"]?.ToString() ?? "",
            CourseName = row["course_name"]?.ToString() ?? "",
            CourseDesc = row["course_desc"] != DBNull.Value ? row["course_desc"]?.ToString() : null,
            CourseUnits = Convert.ToInt32(row["course_units"]),
            ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : null,
            ProgramName = row["program_name"]?.ToString() ?? "N/A"
        });
    }
}
