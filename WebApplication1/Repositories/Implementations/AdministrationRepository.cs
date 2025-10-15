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
        string query = @"SELECT admin_id, user_id, admin_code, admin_created_at FROM administration;";
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
                         FROM administration WHERE admin_id = @id;";
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
        string query = @"INSERT INTO administration (user_id, admin_code, admin_created_at)
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
        string query = @"UPDATE administration 
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
        string query = "DELETE FROM administration WHERE admin_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", adminId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int adminId)
    {
        string query = "SELECT COUNT(*) FROM administration WHERE admin_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", adminId } };
        return await _db.RecordExistsAsync(query, parameters);
    }

    public async Task<Administration?> GetByUserIdAsync(int userId)
    {
        string query = @"SELECT admin_id, user_id, admin_code, admin_created_at 
                         FROM administration WHERE user_id = @userId;";
        var parameters = new Dictionary<string, object> { { "@userId", userId } };

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

    public async Task<string> GenerateNextAdminCodeAsync()
    {
        string query = @"SELECT admin_code FROM administration 
                         WHERE admin_code LIKE 'ADM-%' 
                         ORDER BY admin_code DESC 
                         LIMIT 1;";
        
        var dt = await _db.ExecuteQueryAsync(query);
        
        if (dt.Rows.Count == 0)
        {
            // First admin code
            return $"ADM-{DateTime.Now.Year}-0001";
        }

        var lastCode = dt.Rows[0]["admin_code"]?.ToString();
        if (string.IsNullOrEmpty(lastCode))
        {
            return $"ADM-{DateTime.Now.Year}-0001";
        }

        // Parse the last code (format: ADM-YYYY-NNNN)
        var parts = lastCode.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
        {
            var currentYear = DateTime.Now.Year;
            var lastYear = int.TryParse(parts[1], out int year) ? year : currentYear;

            // If it's a new year, reset to 0001
            if (currentYear > lastYear)
            {
                return $"ADM-{currentYear}-0001";
            }

            // Increment the number
            var nextNumber = lastNumber + 1;
            return $"ADM-{currentYear}-{nextNumber:D4}";
        }

        // Fallback
        return $"ADM-{DateTime.Now.Year}-0001";
    }
}
