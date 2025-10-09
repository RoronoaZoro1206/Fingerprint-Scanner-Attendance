using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class AdministrationRepository : IAdministrationRepository
{
    private readonly DatabaseHelper _db;

    public AdministrationRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Administration>> GetAllAsync()
    {
        string query = @"SELECT admin_id, user_id, admin_code, admin_created_at FROM administrations;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Administration
        {
            AdminId = Convert.ToInt32(row["admin_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            AdminCode = row["admin_code"]?.ToString(),
            AdminCreatedAt = Convert.ToDateTime(row["admin_created_at"])
        });
    }

    public async Task<Administration?> GetByIdAsync(int adminId)
    {
        string query = @"SELECT admin_id, user_id, admin_code, admin_created_at 
                         FROM administrations WHERE admin_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", adminId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Administration
        {
            AdminId = Convert.ToInt32(row["admin_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            AdminCode = row["admin_code"]?.ToString(),
            AdminCreatedAt = Convert.ToDateTime(row["admin_created_at"])
        };
    }

    public async Task<bool> AddAsync(Administration admin)
    {
        string query = @"INSERT INTO administrations (user_id, admin_code, admin_created_at)
                         VALUES (@user, @code, @created);";
        var parameters = new Dictionary<string, object>
        {
            { "@user", admin.UserId },
            { "@code", admin.AdminCode ?? (object)DBNull.Value },
            { "@created", admin.AdminCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Administration admin)
    {
        string query = @"UPDATE administrations 
                         SET user_id = @user, admin_code = @code, admin_created_at = @created
                         WHERE admin_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", admin.AdminId },
            { "@user", admin.UserId },
            { "@code", admin.AdminCode ?? (object)DBNull.Value },
            { "@created", admin.AdminCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int adminId)
    {
        string query = "DELETE FROM administrations WHERE admin_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", adminId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int adminId)
    {
        string query = "SELECT COUNT(*) FROM administrations WHERE admin_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", adminId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
