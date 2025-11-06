using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    public class AuthenticationAuditRepository : IAuthenticationAuditRepository
    {
        private readonly DatabaseHelper _db;

        public AuthenticationAuditRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<AuthenticationAudit>> GetAllAsync()
        {
            string query = @"
                SELECT auth_audit_id, user_id, auth_action, auth_details, 
                       auth_action_time, auth_ip_address, auth_user_agent
                FROM authentication_audit
                ORDER BY auth_action_time DESC;";
            
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new AuthenticationAudit
            {
                AuthAuditId = Convert.ToInt32(row["auth_audit_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                AuthAction = row["auth_action"].ToString() ?? "",
                AuthDetails = row["auth_details"]?.ToString(),
                AuthActionTime = Convert.ToDateTime(row["auth_action_time"]),
                AuthIpAddress = row["auth_ip_address"]?.ToString(),
                AuthUserAgent = row["auth_user_agent"]?.ToString()
            });
        }

        public async Task<AuthenticationAudit?> GetByIdAsync(int authAuditId)
        {
            string query = @"
                SELECT auth_audit_id, user_id, auth_action, auth_details, 
                       auth_action_time, auth_ip_address, auth_user_agent
                FROM authentication_audit 
                WHERE auth_audit_id = @id;";
            
            var parameters = new Dictionary<string, object> { { "@id", authAuditId } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);
            
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new AuthenticationAudit
            {
                AuthAuditId = Convert.ToInt32(row["auth_audit_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                AuthAction = row["auth_action"].ToString() ?? "",
                AuthDetails = row["auth_details"]?.ToString(),
                AuthActionTime = Convert.ToDateTime(row["auth_action_time"]),
                AuthIpAddress = row["auth_ip_address"]?.ToString(),
                AuthUserAgent = row["auth_user_agent"]?.ToString()
            };
        }

        public async Task<IEnumerable<AuthenticationAudit>> GetByUserIdAsync(int userId)
        {
            string query = @"
                SELECT auth_audit_id, user_id, auth_action, auth_details, 
                       auth_action_time, auth_ip_address, auth_user_agent
                FROM authentication_audit 
                WHERE user_id = @userId
                ORDER BY auth_action_time DESC;";
            
            var parameters = new Dictionary<string, object> { { "@userId", userId } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new AuthenticationAudit
            {
                AuthAuditId = Convert.ToInt32(row["auth_audit_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                AuthAction = row["auth_action"].ToString() ?? "",
                AuthDetails = row["auth_details"]?.ToString(),
                AuthActionTime = Convert.ToDateTime(row["auth_action_time"]),
                AuthIpAddress = row["auth_ip_address"]?.ToString(),
                AuthUserAgent = row["auth_user_agent"]?.ToString()
            });
        }

        public async Task<IEnumerable<AuthenticationAudit>> GetByActionAsync(string action)
        {
            string query = @"
                SELECT auth_audit_id, user_id, auth_action, auth_details, 
                       auth_action_time, auth_ip_address, auth_user_agent
                FROM authentication_audit 
                WHERE auth_action = @action
                ORDER BY auth_action_time DESC;";
            
            var parameters = new Dictionary<string, object> { { "@action", action } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new AuthenticationAudit
            {
                AuthAuditId = Convert.ToInt32(row["auth_audit_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                AuthAction = row["auth_action"].ToString() ?? "",
                AuthDetails = row["auth_details"]?.ToString(),
                AuthActionTime = Convert.ToDateTime(row["auth_action_time"]),
                AuthIpAddress = row["auth_ip_address"]?.ToString(),
                AuthUserAgent = row["auth_user_agent"]?.ToString()
            });
        }

        public async Task<IEnumerable<AuthenticationAudit>> GetRecentAsync(int count = 50)
        {
            string query = @"
                SELECT auth_audit_id, user_id, auth_action, auth_details, 
                       auth_action_time, auth_ip_address, auth_user_agent
                FROM authentication_audit
                ORDER BY auth_action_time DESC
                LIMIT @count;";
            
            var parameters = new Dictionary<string, object> { { "@count", count } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new AuthenticationAudit
            {
                AuthAuditId = Convert.ToInt32(row["auth_audit_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                AuthAction = row["auth_action"].ToString() ?? "",
                AuthDetails = row["auth_details"]?.ToString(),
                AuthActionTime = Convert.ToDateTime(row["auth_action_time"]),
                AuthIpAddress = row["auth_ip_address"]?.ToString(),
                AuthUserAgent = row["auth_user_agent"]?.ToString()
            });
        }

        public async Task<bool> AddAsync(AuthenticationAudit audit)
        {
            string query = @"
                INSERT INTO authentication_audit 
                (user_id, auth_action, auth_details, auth_action_time, auth_ip_address, auth_user_agent)
                VALUES (@userId, @action, @details, @time, @ip, @userAgent);";
            
            var parameters = new Dictionary<string, object>
            {
                { "@userId", audit.UserId },
                { "@action", audit.AuthAction },
                { "@details", audit.AuthDetails ?? (object)DBNull.Value },
                { "@time", audit.AuthActionTime },
                { "@ip", audit.AuthIpAddress ?? (object)DBNull.Value },
                { "@userAgent", audit.AuthUserAgent ?? (object)DBNull.Value }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int authAuditId)
        {
            string query = "DELETE FROM authentication_audit WHERE auth_audit_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", authAuditId } };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int authAuditId)
        {
            string query = "SELECT COUNT(*) FROM authentication_audit WHERE auth_audit_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", authAuditId } };
            return await _db.RecordExistsAsync(query, parameters);
        }
    }
}
