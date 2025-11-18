using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;

public class StudentIdPoolRepository : IStudentIdPoolRepository
{
    private readonly DatabaseHelper _db;

    public StudentIdPoolRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<StudentIdPool>> GetAllAsync()
    {
        string query = "SELECT * FROM student_id_pool ORDER BY pool_id;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new StudentIdPool
        {
            PoolId = Convert.ToInt32(row["pool_id"]),
            StudentIdCode = row["student_id_code"].ToString() ?? "",
            IsUsed = Convert.ToBoolean(row["is_used"]),
            GeneratedBy = row["generated_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["generated_by"]),
            GeneratedAt = Convert.ToDateTime(row["generated_at"]),
            UsedAt = row["used_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["used_at"]),
            Remarks = row["remarks"]?.ToString()
        });
    }

    public async Task<IEnumerable<StudentIdPool>> GetAvailableAsync()
    {
        string query = "SELECT * FROM student_id_pool WHERE is_used = FALSE ORDER BY student_id_code;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new StudentIdPool
        {
            PoolId = Convert.ToInt32(row["pool_id"]),
            StudentIdCode = row["student_id_code"].ToString() ?? "",
            IsUsed = Convert.ToBoolean(row["is_used"]),
            GeneratedBy = row["generated_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["generated_by"]),
            GeneratedAt = Convert.ToDateTime(row["generated_at"]),
            UsedAt = row["used_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["used_at"]),
            Remarks = row["remarks"]?.ToString()
        });
    }

    public async Task<StudentIdPool?> GetByIdAsync(int poolId)
    {
        string query = "SELECT * FROM student_id_pool WHERE pool_id = @id;";
        var dt = await _db.ExecuteQueryAsync(query, new() { { "@id", poolId } });
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new StudentIdPool
        {
            PoolId = Convert.ToInt32(row["pool_id"]),
            StudentIdCode = row["student_id_code"].ToString() ?? "",
            IsUsed = Convert.ToBoolean(row["is_used"]),
            GeneratedBy = row["generated_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["generated_by"]),
            GeneratedAt = Convert.ToDateTime(row["generated_at"]),
            UsedAt = row["used_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["used_at"]),
            Remarks = row["remarks"]?.ToString()
        };
    }

    public async Task<StudentIdPool?> GetByCodeAsync(string studentIdCode)
    {
        string query = "SELECT * FROM student_id_pool WHERE student_id_code = @code;";
        var dt = await _db.ExecuteQueryAsync(query, new() { { "@code", studentIdCode } });
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new StudentIdPool
        {
            PoolId = Convert.ToInt32(row["pool_id"]),
            StudentIdCode = row["student_id_code"].ToString() ?? "",
            IsUsed = Convert.ToBoolean(row["is_used"]),
            GeneratedBy = row["generated_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["generated_by"]),
            GeneratedAt = Convert.ToDateTime(row["generated_at"]),
            UsedAt = row["used_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["used_at"]),
            Remarks = row["remarks"]?.ToString()
        };
    }

    public async Task<bool> AddAsync(StudentIdPool pool)
    {
        string query = @"
            INSERT INTO student_id_pool 
            (student_id_code, is_used, generated_by, generated_at, remarks)
            VALUES (@code, @used, @genBy, @genAt, @remarks);";

        var parameters = new Dictionary<string, object>
        {
            { "@code", pool.StudentIdCode },
            { "@used", pool.IsUsed },
            { "@genBy", pool.GeneratedBy ?? (object)DBNull.Value },
            { "@genAt", pool.GeneratedAt },
            { "@remarks", pool.Remarks ?? (object)DBNull.Value }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> MarkAsUsedAsync(int poolId)
    {
        string query = @"
            UPDATE student_id_pool 
            SET is_used = TRUE, used_at = NOW() 
            WHERE pool_id = @id;";
        return await _db.ExecuteNonQueryAsync(query, new() { { "@id", poolId } }) > 0;
    }

    // ✅ NEW: Release student ID back to pool (mark as available again)
    public async Task<bool> ReleaseStudentIdAsync(int poolId, string releaseReason)
    {
        string query = @"
            UPDATE student_id_pool 
            SET is_used = FALSE, 
                used_at = NULL,
                remarks = @reason
            WHERE pool_id = @id;";
        
        var parameters = new Dictionary<string, object>
        {
            { "@id", poolId },
            { "@reason", releaseReason }
        };
        
        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateRemarksAsync(int poolId, string remarks)
    {
        string query = "UPDATE student_id_pool SET remarks = @remarks WHERE pool_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", poolId },
            { "@remarks", remarks }
        };
        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int poolId)
    {
        string query = "DELETE FROM student_id_pool WHERE pool_id = @id;";
        return await _db.ExecuteNonQueryAsync(query, new() { { "@id", poolId } }) > 0;
    }

    public async Task<bool> ExistsAsync(string studentIdCode)
    {
        string query = "SELECT COUNT(*) FROM student_id_pool WHERE student_id_code = @code;";
        return await _db.RecordExistsAsync(query, new() { { "@code", studentIdCode } });
    }
}
