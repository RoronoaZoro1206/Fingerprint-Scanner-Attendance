using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class UserRepository : IUserRepository
{
    private readonly DatabaseHelper _db;

    public UserRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        string query = "SELECT * FROM users ORDER BY user_id;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new User
        {
            UserId = Convert.ToInt32(row["user_id"]),
            UserProfileImage = row["user_profile_image"].ToString() ?? "default_profile.png",
            UserFirstName = row["user_first_name"].ToString() ?? "",
            UserMiddleName = row["user_middle_name"]?.ToString(),
            UserLastName = row["user_last_name"].ToString() ?? "",
            UserEmail = row["user_email"].ToString() ?? "",
            UserPassword = row["user_password"].ToString() ?? "",
            RoleId = Convert.ToInt32(row["role_id"]),
            UserCreatedAt = Convert.ToDateTime(row["user_created_at"]),
            UserUpdatedAt = Convert.ToDateTime(row["user_updated_at"]),
            IsActive = Convert.ToBoolean(row["is_active"])
        });
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        string query = "SELECT * FROM users WHERE user_id = @user_id;";
        var parameters = new Dictionary<string, object> { { "@user_id", userId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new User
        {
            UserId = Convert.ToInt32(row["user_id"]),
            UserProfileImage = row["user_profile_image"].ToString() ?? "default_profile.png",
            UserFirstName = row["user_first_name"].ToString() ?? "",
            UserMiddleName = row["user_middle_name"]?.ToString(),
            UserLastName = row["user_last_name"].ToString() ?? "",
            UserEmail = row["user_email"].ToString() ?? "",
            UserPassword = row["user_password"].ToString() ?? "",
            RoleId = Convert.ToInt32(row["role_id"]),
            UserCreatedAt = Convert.ToDateTime(row["user_created_at"]),
            UserUpdatedAt = Convert.ToDateTime(row["user_updated_at"]),
            IsActive = Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<bool> AddAsync(User user)
    {
        string query = @"
            INSERT INTO users 
            (user_profile_image, user_first_name, user_middle_name, user_last_name, 
             user_email, user_password, role_id, user_created_at, user_updated_at, is_active)
            VALUES (@img, @fname, @mname, @lname, @email, @pass, @role, @created, @updated, @active);";

        var parameters = new Dictionary<string, object>
        {
            { "@img", user.UserProfileImage },
            { "@fname", user.UserFirstName },
            { "@mname", user.UserMiddleName ?? (object)DBNull.Value },
            { "@lname", user.UserLastName },
            { "@email", user.UserEmail },
            { "@pass", user.UserPassword },
            { "@role", user.RoleId },
            { "@created", user.UserCreatedAt },
            { "@updated", user.UserUpdatedAt },
            { "@active", user.IsActive }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(User user)
    {
        string query = @"
            UPDATE users SET
                user_profile_image = @img,
                user_first_name = @fname,
                user_middle_name = @mname,
                user_last_name = @lname,
                user_email = @email,
                user_password = @pass,
                role_id = @role,
                user_updated_at = @updated,
                is_active = @active
            WHERE user_id = @id;";

        var parameters = new Dictionary<string, object>
        {
            { "@id", user.UserId },
            { "@img", user.UserProfileImage },
            { "@fname", user.UserFirstName },
            { "@mname", user.UserMiddleName ?? (object)DBNull.Value },
            { "@lname", user.UserLastName },
            { "@email", user.UserEmail },
            { "@pass", user.UserPassword },
            { "@role", user.RoleId },
            { "@updated", user.UserUpdatedAt },
            { "@active", user.IsActive }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int userId)
    {
        string query = "DELETE FROM users WHERE user_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", userId } };
        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int userId)
    {
        string query = "SELECT COUNT(*) FROM users WHERE user_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", userId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
