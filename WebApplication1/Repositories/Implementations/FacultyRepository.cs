using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class FacultyRepository : IFacultyRepository
{
    private readonly DatabaseHelper _db;

    public FacultyRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Faculty>> GetAllAsync()
    {
        string query = "SELECT * FROM faculties ORDER BY fac_id;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Faculty
        {
            FacId = Convert.ToInt32(row["fac_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            FacDepartment = row["fac_department"]?.ToString(),
            FacRank = row["fac_rank"]?.ToString(),
            FacCreatedAt = Convert.ToDateTime(row["fac_created_at"])
        });
    }

    public async Task<Faculty?> GetByIdAsync(int facId)
    {
        string query = "SELECT * FROM faculties WHERE fac_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", facId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Faculty
        {
            FacId = Convert.ToInt32(row["fac_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            FacDepartment = row["fac_department"]?.ToString(),
            FacRank = row["fac_rank"]?.ToString(),
            FacCreatedAt = Convert.ToDateTime(row["fac_created_at"])
        };
    }

    public async Task<bool> AddAsync(Faculty faculty)
    {
        string query = @"
            INSERT INTO faculties (user_id, fac_department, fac_rank, fac_created_at)
            VALUES (@user, @dept, @rank, @created);";

        var parameters = new Dictionary<string, object>
        {
            { "@user", faculty.UserId },
            { "@dept", faculty.FacDepartment ?? (object)DBNull.Value },
            { "@rank", faculty.FacRank ?? (object)DBNull.Value },
            { "@created", faculty.FacCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Faculty faculty)
    {
        string query = @"
            UPDATE faculties
            SET fac_department = @dept,
                fac_rank = @rank
            WHERE fac_id = @id;";

        var parameters = new Dictionary<string, object>
        {
            { "@id", faculty.FacId },
            { "@dept", faculty.FacDepartment ?? (object)DBNull.Value },
            { "@rank", faculty.FacRank ?? (object)DBNull.Value }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int facId)
    {
        string query = "DELETE FROM faculties WHERE fac_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", facId } };
        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }
}

