using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class AdministrationAuditRepository : IAdministrationAuditRepository
{
    private readonly DatabaseHelper _db;

    public AdministrationAuditRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<AdministrationAudit>> GetAllAsync()
    {
        string query = @"SELECT aud_id, admin_id, aud_action, aud_details, aud_action_time 
                         FROM administration_audit;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new AdministrationAudit
        {
            AudId = Convert.ToInt32(row["aud_id"]),
            AdminId = Convert.ToInt32(row["admin_id"]),
            AudAction = row["aud_action"].ToString() ?? "",
            AudDetails = row["aud_details"]?.ToString(),
            AudActionTime = Convert.ToDateTime(row["aud_action_time"])
        });
    }

    public async Task<AdministrationAudit?> GetByIdAsync(int audId)
    {
        string query = @"SELECT aud_id, admin_id, aud_action, aud_details, aud_action_time 
                         FROM administration_audit WHERE aud_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", audId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new AdministrationAudit
        {
            AudId = Convert.ToInt32(row["aud_id"]),
            AdminId = Convert.ToInt32(row["admin_id"]),
            AudAction = row["aud_action"].ToString() ?? "",
            AudDetails = row["aud_details"]?.ToString(),
            AudActionTime = Convert.ToDateTime(row["aud_action_time"])
        };
    }

    public async Task<bool> AddAsync(AdministrationAudit audit)
    {
        string query = @"INSERT INTO administration_audit (admin_id, aud_action, aud_details, aud_action_time)
                         VALUES (@admin, @action, @details, @time);";
        var parameters = new Dictionary<string, object>
        {
            { "@admin", audit.AdminId },
            { "@action", audit.AudAction },
            { "@details", audit.AudDetails ?? (object)DBNull.Value },
            { "@time", audit.AudActionTime }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(AdministrationAudit audit)
    {
        string query = @"UPDATE administration_audit 
                         SET admin_id = @admin, aud_action = @action, aud_details = @details, aud_action_time = @time
                         WHERE aud_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", audit.AudId },
            { "@admin", audit.AdminId },
            { "@action", audit.AudAction },
            { "@details", audit.AudDetails ?? (object)DBNull.Value },
            { "@time", audit.AudActionTime }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int audId)
    {
        string query = "DELETE FROM administration_audit WHERE aud_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", audId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int audId)
    {
        string query = "SELECT COUNT(*) FROM administration_audit WHERE aud_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", audId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
