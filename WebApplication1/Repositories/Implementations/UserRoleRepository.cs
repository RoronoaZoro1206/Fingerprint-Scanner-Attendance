using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class UserRoleRepository : IUserRoleRepository
{
    private readonly DatabaseHelper _db;

    public UserRoleRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<UserRole>> GetAllAsync()
    {
        string query = "SELECT role_id, role_name FROM user_roles ORDER BY role_id;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new UserRole
        {
            RoleId = Convert.ToInt32(row["role_id"]),
            RoleName = row["role_name"].ToString() ?? ""
        });
    }

    public async Task<UserRole?> GetByIdAsync(int roleId)
    {
        string query = "SELECT role_id, role_name FROM user_roles WHERE role_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", roleId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new UserRole
        {
            RoleId = Convert.ToInt32(row["role_id"]),
            RoleName = row["role_name"].ToString() ?? ""
        };
    }
}