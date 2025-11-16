using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for attendance notifications
    /// Manages attendance_notifications table operations
    /// </summary>
    public class AttendanceNotificationRepository : IAttendanceNotificationRepository
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<AttendanceNotificationRepository> _logger;

        public AttendanceNotificationRepository(
            DatabaseHelper db,
            ILogger<AttendanceNotificationRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<AttendanceNotification>> GetAllAsync()
        {
            const string query = @"
                SELECT notification_id, user_id, notification_type, title, message,
                       related_entity_type, related_entity_id, is_read, created_at
                FROM attendance_notifications
                ORDER BY created_at DESC";

            var dt = await _db.ExecuteQueryAsync(query);
            return MapFromDataTable(dt);
        }

        public async Task<AttendanceNotification?> GetByIdAsync(int notificationId)
        {
            const string query = @"
                SELECT notification_id, user_id, notification_type, title, message,
                       related_entity_type, related_entity_id, is_read, created_at
                FROM attendance_notifications
                WHERE notification_id = @id";

            var parameters = new Dictionary<string, object>
            {
                { "@id", notificationId }
            };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return dt.Rows.Count > 0 ? MapFromDataRow(dt.Rows[0]) : null;
        }

        public async Task<IEnumerable<AttendanceNotification>> GetByUserIdAsync(int userId)
        {
            const string query = @"
                SELECT notification_id, user_id, notification_type, title, message,
                       related_entity_type, related_entity_id, is_read, created_at
                FROM attendance_notifications
                WHERE user_id = @user_id
                ORDER BY created_at DESC
                LIMIT 50";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", userId }
            };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return MapFromDataTable(dt);
        }

        public async Task<IEnumerable<AttendanceNotification>> GetUnreadByUserIdAsync(int userId)
        {
            const string query = @"
                SELECT notification_id, user_id, notification_type, title, message,
                       related_entity_type, related_entity_id, is_read, created_at
                FROM attendance_notifications
                WHERE user_id = @user_id AND is_read = FALSE
                ORDER BY created_at DESC";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", userId }
            };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return MapFromDataTable(dt);
        }

        public async Task<bool> AddAsync(AttendanceNotification notification)
        {
            const string query = @"
                INSERT INTO attendance_notifications 
                    (user_id, notification_type, title, message, related_entity_type, 
                     related_entity_id, is_read, created_at)
                VALUES 
                    (@user_id, @type, @title, @message, @entity_type, @entity_id, @is_read, @created_at)";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", notification.UserId },
                { "@type", notification.NotificationType },
                { "@title", notification.Title },
                { "@message", notification.Message },
                { "@entity_type", notification.RelatedEntityType ?? (object)DBNull.Value },
                { "@entity_id", notification.RelatedEntityId ?? (object)DBNull.Value },
                { "@is_read", notification.IsRead },
                { "@created_at", notification.CreatedAt }
            };

            try
            {
                return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding attendance notification");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(AttendanceNotification notification)
        {
            const string query = @"
                UPDATE attendance_notifications
                SET notification_type = @type,
                    title = @title,
                    message = @message,
                    related_entity_type = @entity_type,
                    related_entity_id = @entity_id,
                    is_read = @is_read
                WHERE notification_id = @id";

            var parameters = new Dictionary<string, object>
            {
                { "@id", notification.NotificationId },
                { "@type", notification.NotificationType },
                { "@title", notification.Title },
                { "@message", notification.Message },
                { "@entity_type", notification.RelatedEntityType ?? (object)DBNull.Value },
                { "@entity_id", notification.RelatedEntityId ?? (object)DBNull.Value },
                { "@is_read", notification.IsRead }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            const string query = "DELETE FROM attendance_notifications WHERE notification_id = @id";
            
            var parameters = new Dictionary<string, object>
            {
                { "@id", notificationId }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            const string query = @"
                UPDATE attendance_notifications
                SET is_read = TRUE
                WHERE notification_id = @id";

            var parameters = new Dictionary<string, object>
            {
                { "@id", notificationId }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            const string query = @"
                UPDATE attendance_notifications
                SET is_read = TRUE
                WHERE user_id = @user_id AND is_read = FALSE";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", userId }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            const string query = @"
                SELECT COUNT(*)
                FROM attendance_notifications
                WHERE user_id = @user_id AND is_read = FALSE";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", userId }
            };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }

        private IEnumerable<AttendanceNotification> MapFromDataTable(DataTable dt)
        {
            var notifications = new List<AttendanceNotification>();
            foreach (DataRow row in dt.Rows)
            {
                notifications.Add(MapFromDataRow(row));
            }
            return notifications;
        }

        private AttendanceNotification MapFromDataRow(DataRow row)
        {
            return new AttendanceNotification
            {
                NotificationId = Convert.ToInt32(row["notification_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                NotificationType = row["notification_type"].ToString() ?? string.Empty,
                Title = row["title"].ToString() ?? string.Empty,
                Message = row["message"].ToString() ?? string.Empty,
                RelatedEntityType = row["related_entity_type"] == DBNull.Value 
                    ? null 
                    : row["related_entity_type"].ToString(),
                RelatedEntityId = row["related_entity_id"] == DBNull.Value 
                    ? null 
                    : Convert.ToInt32(row["related_entity_id"]),
                IsRead = Convert.ToBoolean(row["is_read"]),
                CreatedAt = Convert.ToDateTime(row["created_at"])
            };
        }
    }
}
