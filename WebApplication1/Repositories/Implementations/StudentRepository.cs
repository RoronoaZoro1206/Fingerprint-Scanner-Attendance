using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class StudentRepository : IStudentRepository
{
    private readonly DatabaseHelper _db;

    public StudentRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        string query = "SELECT * FROM students ORDER BY stud_id;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Student
        {
            StudId = Convert.ToInt32(row["stud_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            StudNumber = row["stud_number"].ToString() ?? "",
            ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : null,
            StudYearLevel = row["stud_year_level"] != DBNull.Value ? Convert.ToInt32(row["stud_year_level"]) : null,
            StudSection = row["stud_section"]?.ToString(),
            StudCreatedAt = Convert.ToDateTime(row["stud_created_at"])
        });
    }

    public async Task<Student?> GetByIdAsync(int studId)
    {
        string query = "SELECT * FROM students WHERE stud_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", studId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Student
        {
            StudId = Convert.ToInt32(row["stud_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            StudNumber = row["stud_number"].ToString() ?? "",
            ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : null,
            StudYearLevel = row["stud_year_level"] != DBNull.Value ? Convert.ToInt32(row["stud_year_level"]) : null,
            StudSection = row["stud_section"]?.ToString(),
            StudCreatedAt = Convert.ToDateTime(row["stud_created_at"])
        };
    }

    public async Task<bool> AddAsync(Student student)
    {
        string query = @"
            INSERT INTO students (user_id, stud_number, program_id, stud_year_level, stud_section, stud_created_at)
            VALUES (@user, @num, @program, @year, @section, @created);";

        var parameters = new Dictionary<string, object>
        {
            { "@user", student.UserId },
            { "@num", student.StudNumber },
            { "@program", student.ProgramId ?? (object)DBNull.Value },
            { "@year", student.StudYearLevel ?? (object)DBNull.Value },
            { "@section", student.StudSection ?? (object)DBNull.Value },
            { "@created", student.StudCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Student student)
    {
        string query = @"
            UPDATE students
            SET stud_number = @num,
                program_id = @program,
                stud_year_level = @year,
                stud_section = @section
            WHERE stud_id = @id;";

        var parameters = new Dictionary<string, object>
        {
            { "@id", student.StudId },
            { "@num", student.StudNumber },
            { "@program", student.ProgramId ?? (object)DBNull.Value },
            { "@year", student.StudYearLevel ?? (object)DBNull.Value },
            { "@section", student.StudSection ?? (object)DBNull.Value }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int studId)
    {
        string query = "DELETE FROM students WHERE stud_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", studId } };
        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }
}
