using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class ScanLogRepository : IScanLogRepository
{
    private readonly DatabaseHelper _db;

    public ScanLogRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ScanLog>> GetAllAsync()
    {
        string query = @"SELECT scan_id, user_id, scan_time, scan_success, scan_log_message, scan_created_at
                         FROM scan_logs;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new ScanLog
        {
            ScanId = Convert.ToInt32(row["scan_id"]),
            UserId = row["user_id"] == DBNull.Value ? null : Convert.ToInt32(row["user_id"]),
            ScanTime = Convert.ToDateTime(row["scan_time"]),
            ScanSuccess = row["scan_success"] == DBNull.Value ? null : Convert.ToBoolean(row["scan_success"]),
            ScanLogMessage = row["scan_log_message"]?.ToString(),
            ScanCreatedAt = Convert.ToDateTime(row["scan_created_at"])
        });
    }

    public async Task<ScanLog?> GetByIdAsync(int scanId)
    {
        string query = @"SELECT scan_id, user_id, scan_time, scan_success, scan_log_message, scan_created_at
                         FROM scan_logs WHERE scan_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", scanId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new ScanLog
        {
            ScanId = Convert.ToInt32(row["scan_id"]),
            UserId = row["user_id"] == DBNull.Value ? null : Convert.ToInt32(row["user_id"]),
            ScanTime = Convert.ToDateTime(row["scan_time"]),
            ScanSuccess = row["scan_success"] == DBNull.Value ? null : Convert.ToBoolean(row["scan_success"]),
            ScanLogMessage = row["scan_log_message"]?.ToString(),
            ScanCreatedAt = Convert.ToDateTime(row["scan_created_at"])
        };
    }

    public async Task<bool> AddAsync(ScanLog scanLog)
    {
        string query = @"INSERT INTO scan_logs (user_id, scan_time, scan_success, scan_log_message, scan_created_at)
                         VALUES (@user, @time, @success, @message, @created);";
        var parameters = new Dictionary<string, object>
        {
            { "@user", scanLog.UserId ?? (object)DBNull.Value },
            { "@time", scanLog.ScanTime },
            { "@success", scanLog.ScanSuccess ?? (object)DBNull.Value },
            { "@message", scanLog.ScanLogMessage ?? (object)DBNull.Value },
            { "@created", scanLog.ScanCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(ScanLog scanLog)
    {
        string query = @"UPDATE scan_logs 
                         SET user_id = @user, scan_time = @time, scan_success = @success, 
                             scan_log_message = @message, scan_created_at = @created
                         WHERE scan_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", scanLog.ScanId },
            { "@user", scanLog.UserId ?? (object)DBNull.Value },
            { "@time", scanLog.ScanTime },
            { "@success", scanLog.ScanSuccess ?? (object)DBNull.Value },
            { "@message", scanLog.ScanLogMessage ?? (object)DBNull.Value },
            { "@created", scanLog.ScanCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int scanId)
    {
        string query = "DELETE FROM scan_logs WHERE scan_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", scanId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int scanId)
    {
        string query = "SELECT COUNT(*) FROM scan_logs WHERE scan_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", scanId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
