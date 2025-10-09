using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly DatabaseHelper _db;

    public EnrollmentRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Enrollment>> GetAllAsync()
    {
        string query = @"SELECT enroll_id, stud_id, class_id, enroll_at 
                         FROM enrollments;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Enrollment
        {
            EnrollId = Convert.ToInt32(row["enroll_id"]),
            StudId = Convert.ToInt32(row["stud_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            EnrollAt = Convert.ToDateTime(row["enroll_at"])
        });
    }

    public async Task<Enrollment?> GetByIdAsync(int enrollId)
    {
        string query = @"SELECT enroll_id, stud_id, class_id, enroll_at 
                         FROM enrollments WHERE enroll_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", enrollId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Enrollment
        {
            EnrollId = Convert.ToInt32(row["enroll_id"]),
            StudId = Convert.ToInt32(row["stud_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            EnrollAt = Convert.ToDateTime(row["enroll_at"])
        };
    }

    public async Task<bool> AddAsync(Enrollment enroll)
    {
        string query = @"INSERT INTO enrollments (stud_id, class_id, enroll_at)
                         VALUES (@stud, @class, @at);";
        var parameters = new Dictionary<string, object>
        {
            { "@stud", enroll.StudId },
            { "@class", enroll.ClassId },
            { "@at", enroll.EnrollAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Enrollment enroll)
    {
        string query = @"UPDATE enrollments 
                         SET stud_id = @stud, class_id = @class, enroll_at = @at 
                         WHERE enroll_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", enroll.EnrollId },
            { "@stud", enroll.StudId },
            { "@class", enroll.ClassId },
            { "@at", enroll.EnrollAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int enrollId)
    {
        string query = "DELETE FROM enrollments WHERE enroll_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", enrollId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int enrollId)
    {
        string query = "SELECT COUNT(*) FROM enrollments WHERE enroll_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", enrollId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
