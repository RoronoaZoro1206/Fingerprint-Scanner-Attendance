using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class FingerprintTemplateRepository : IFingerprintTemplateRepository
{
    private readonly DatabaseHelper _db;

    public FingerprintTemplateRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<FingerprintTemplate>> GetAllAsync()
    {
        string query = @"SELECT ftemp_id, user_id, ftemp_data, ftemp_name, ftemp_created_at 
                         FROM fingerprint_templates;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new FingerprintTemplate
        {
            FtempId = Convert.ToInt32(row["ftemp_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            FtempData = (byte[])row["ftemp_data"],
            FtempName = row["ftemp_name"]?.ToString(),
            FtempCreatedAt = Convert.ToDateTime(row["ftemp_created_at"])
        });
    }

    public async Task<FingerprintTemplate?> GetByIdAsync(int ftempId)
    {
        string query = @"SELECT ftemp_id, user_id, ftemp_data, ftemp_name, ftemp_created_at 
                         FROM fingerprint_templates WHERE ftemp_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", ftempId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new FingerprintTemplate
        {
            FtempId = Convert.ToInt32(row["ftemp_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            FtempData = (byte[])row["ftemp_data"],
            FtempName = row["ftemp_name"]?.ToString(),
            FtempCreatedAt = Convert.ToDateTime(row["ftemp_created_at"])
        };
    }

    public async Task<bool> AddAsync(FingerprintTemplate template)
    {
        string query = @"INSERT INTO fingerprint_templates (user_id, ftemp_data, ftemp_name, ftemp_created_at)
                         VALUES (@user, @data, @name, @created);";
        var parameters = new Dictionary<string, object>
        {
            { "@user", template.UserId },
            { "@data", template.FtempData },
            { "@name", template.FtempName ?? (object)DBNull.Value },
            { "@created", template.FtempCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(FingerprintTemplate template)
    {
        string query = @"UPDATE fingerprint_templates 
                         SET user_id = @user, ftemp_data = @data, ftemp_name = @name, ftemp_created_at = @created 
                         WHERE ftemp_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", template.FtempId },
            { "@user", template.UserId },
            { "@data", template.FtempData },
            { "@name", template.FtempName ?? (object)DBNull.Value },
            { "@created", template.FtempCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int ftempId)
    {
        string query = "DELETE FROM fingerprint_templates WHERE ftemp_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", ftempId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int ftempId)
    {
        string query = "SELECT COUNT(*) FROM fingerprint_templates WHERE ftemp_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", ftempId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
