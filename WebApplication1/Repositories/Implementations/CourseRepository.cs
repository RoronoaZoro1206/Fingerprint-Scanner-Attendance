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