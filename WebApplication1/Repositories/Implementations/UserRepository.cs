using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
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
            UserMiddleName = row["user_middle_name"] == DBNull.Value ? null : row["user_middle_name"].ToString(),
            UserLastName = row["user_last_name"].ToString() ?? "",
            UserGender = row["user_gender"] == DBNull.Value ? null : row["user_gender"].ToString(),
            UserPhone = row["user_phone"] == DBNull.Value ? null : row["user_phone"].ToString(),
            UserAddress = row["user_address"] == DBNull.Value ? null : row["user_address"].ToString(),
            UserBirthdate = row["user_birthdate"] == DBNull.Value ? null : Convert.ToDateTime(row["user_birthdate"]),
            UserEmail = row["user_email"].ToString() ?? "",
            UserPassword = row["user_password"].ToString() ?? "",
            RoleId = Convert.ToInt32(row["role_id"]),
            UserCreatedAt = Convert.ToDateTime(row["user_created_at"]),
            UserUpdatedAt = Convert.ToDateTime(row["user_updated_at"]),
            UserLastLogin = row["user_last_login"] == DBNull.Value ? null : Convert.ToDateTime(row["user_last_login"]),
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
            UserMiddleName = row["user_middle_name"] == DBNull.Value ? null : row["user_middle_name"].ToString(),
            UserLastName = row["user_last_name"].ToString() ?? "",
            UserGender = row["user_gender"] == DBNull.Value ? null : row["user_gender"].ToString(),
            UserPhone = row["user_phone"] == DBNull.Value ? null : row["user_phone"].ToString(),
            UserAddress = row["user_address"] == DBNull.Value ? null : row["user_address"].ToString(),
            UserBirthdate = row["user_birthdate"] == DBNull.Value ? null : Convert.ToDateTime(row["user_birthdate"]),
            UserEmail = row["user_email"].ToString() ?? "",
            UserPassword = row["user_password"].ToString() ?? "",
            RoleId = Convert.ToInt32(row["role_id"]),
            UserCreatedAt = Convert.ToDateTime(row["user_created_at"]),
            UserUpdatedAt = Convert.ToDateTime(row["user_updated_at"]),
            UserLastLogin = row["user_last_login"] == DBNull.Value ? null : Convert.ToDateTime(row["user_last_login"]),
            IsActive = Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        string query = "SELECT * FROM users WHERE LOWER(user_email) = LOWER(@email);";
        var parameters = new Dictionary<string, object> { { "@email", email } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new User
        {
            UserId = Convert.ToInt32(row["user_id"]),
            UserProfileImage = row["user_profile_image"].ToString() ?? "default_profile.png",
            UserFirstName = row["user_first_name"].ToString() ?? "",
            UserMiddleName = row["user_middle_name"] == DBNull.Value ? null : row["user_middle_name"].ToString(),
            UserLastName = row["user_last_name"].ToString() ?? "",
            UserGender = row["user_gender"] == DBNull.Value ? null : row["user_gender"].ToString(),
            UserPhone = row["user_phone"] == DBNull.Value ? null : row["user_phone"].ToString(),
            UserAddress = row["user_address"] == DBNull.Value ? null : row["user_address"].ToString(),
            UserBirthdate = row["user_birthdate"] == DBNull.Value ? null : Convert.ToDateTime(row["user_birthdate"]),
            UserEmail = row["user_email"].ToString() ?? "",
            UserPassword = row["user_password"].ToString() ?? "",
            RoleId = Convert.ToInt32(row["role_id"]),
            UserCreatedAt = Convert.ToDateTime(row["user_created_at"]),
            UserUpdatedAt = Convert.ToDateTime(row["user_updated_at"]),
            UserLastLogin = row["user_last_login"] == DBNull.Value ? null : Convert.ToDateTime(row["user_last_login"]),
            IsActive = Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<bool> AddAsync(User user)
    {
        string query = @"
            INSERT INTO users 
            (user_profile_image, user_first_name, user_middle_name, user_last_name, 
             user_gender, user_phone, user_address, user_birthdate,
             user_email, user_password, role_id, user_created_at, user_updated_at, is_active)
            VALUES (@img, @fname, @mname, @lname, @gender, @phone, @address, @birthdate,
                    @email, @pass, @role, @created, @updated, @active);";

        var parameters = new Dictionary<string, object>
        {
            { "@img", user.UserProfileImage },
            { "@fname", user.UserFirstName },
            { "@mname", user.UserMiddleName ?? (object)DBNull.Value },
            { "@lname", user.UserLastName },
            { "@gender", user.UserGender ?? (object)DBNull.Value },
            { "@phone", user.UserPhone ?? (object)DBNull.Value },
            { "@address", user.UserAddress ?? (object)DBNull.Value },
            { "@birthdate", user.UserBirthdate ?? (object)DBNull.Value },
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
                user_gender = @gender,
                user_phone = @phone,
                user_address = @address,
                user_birthdate = @birthdate,
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
            { "@gender", user.UserGender ?? (object)DBNull.Value },
            { "@phone", user.UserPhone ?? (object)DBNull.Value },
            { "@address", user.UserAddress ?? (object)DBNull.Value },
            { "@birthdate", user.UserBirthdate ?? (object)DBNull.Value },
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

    public async Task<bool> AdminExistsAsync()
    {
        string query = "SELECT COUNT(*) FROM users WHERE role_id = 1 LIMIT 1;";
        return await _db.RecordExistsAsync(query);
    }

    public async Task<User?> GetByEmailWithRoleCheckAsync(string email, int roleId)
    {
        string query = @"
            SELECT * FROM users 
            WHERE LOWER(user_email) = LOWER(@email) AND role_id = @roleId;";
        
        var parameters = new Dictionary<string, object> 
        { 
            { "@email", email },
            { "@roleId", roleId }
        };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new User
        {
            UserId = Convert.ToInt32(row["user_id"]),
            UserProfileImage = row["user_profile_image"].ToString() ?? "default_profile.png",
            UserFirstName = row["user_first_name"].ToString() ?? "",
            UserMiddleName = row["user_middle_name"] == DBNull.Value ? null : row["user_middle_name"].ToString(),
            UserLastName = row["user_last_name"].ToString() ?? "",
            UserGender = row["user_gender"] == DBNull.Value ? null : row["user_gender"].ToString(),
            UserPhone = row["user_phone"] == DBNull.Value ? null : row["user_phone"].ToString(),
            UserAddress = row["user_address"] == DBNull.Value ? null : row["user_address"].ToString(),
            UserBirthdate = row["user_birthdate"] == DBNull.Value ? null : Convert.ToDateTime(row["user_birthdate"]),
            UserEmail = row["user_email"].ToString() ?? "",
            UserPassword = row["user_password"].ToString() ?? "",
            RoleId = Convert.ToInt32(row["role_id"]),
            UserCreatedAt = Convert.ToDateTime(row["user_created_at"]),
            UserUpdatedAt = Convert.ToDateTime(row["user_updated_at"]),
            UserLastLogin = row["user_last_login"] == DBNull.Value ? null : Convert.ToDateTime(row["user_last_login"]),
            IsActive = Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        string query = "SELECT COUNT(*) FROM users WHERE LOWER(user_email) = LOWER(@email) LIMIT 1;";
        var parameters = new Dictionary<string, object> { { "@email", email } };
        return await _db.RecordExistsAsync(query, parameters);
    }

    public async Task<IEnumerable<UserListDto>> GetUsersWithRolesAsync()
    {
        string query = @"
            SELECT 
                u.user_id as UserId,
                u.user_profile_image as UserProfileImage,
                u.user_first_name as UserFirstName,
                u.user_middle_name as UserMiddleName,
                u.user_last_name as UserLastName,
                u.user_email as UserEmail,
                u.user_phone as UserPhone,
                u.role_id as RoleId,
                r.role_name as RoleName,
                u.is_active as IsActive,
                CASE WHEN u.is_active THEN 'Active' ELSE 'Inactive' END as StatusText
            FROM users u
            INNER JOIN user_roles r ON u.role_id = r.role_id
            ORDER BY u.user_id;";

        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row =>
        {
            var firstName = row["UserFirstName"]?.ToString() ?? "";
            var middleName = row["UserMiddleName"] == DBNull.Value ? "" : row["UserMiddleName"]?.ToString() ?? "";
            var lastName = row["UserLastName"]?.ToString() ?? "";
            
            // Build full name properly
            var fullName = string.IsNullOrWhiteSpace(middleName)
                ? $"{firstName} {lastName}".Trim()
                : $"{firstName} {middleName} {lastName}".Trim();
            
            // Fallback if name is empty
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = "Unnamed User";
            }

            return new UserListDto
            {
                UserId = Convert.ToInt32(row["UserId"]),
                UserProfileImage = row["UserProfileImage"]?.ToString() ?? "default_profile.png",
                FullName = fullName,
                UserEmail = row["UserEmail"]?.ToString() ?? string.Empty,
                UserPhone = row["UserPhone"] == DBNull.Value ? null : row["UserPhone"]?.ToString(),
                RoleId = Convert.ToInt32(row["RoleId"]),
                RoleName = row["RoleName"]?.ToString() ?? "N/A",
                IsActive = Convert.ToBoolean(row["IsActive"]),
                StatusText = row["StatusText"]?.ToString() ?? "Inactive"
            };
        });
    }

    /// <summary>
    /// Updates the user's last login timestamp to the current time
    /// Called after successful authentication
    /// </summary>
    public async Task<bool> UpdateLastLoginAsync(int userId)
    {
        string query = @"
            UPDATE users 
            SET user_last_login = @loginTime 
            WHERE user_id = @userId;";

        var parameters = new Dictionary<string, object>
        {
            { "@userId", userId },
            { "@loginTime", DateTime.Now }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }
}
