using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for attendance notifications
    /// Handles CRUD operations for attendance_notifications table
    /// </summary>
    public interface IAttendanceNotificationRepository
    {
        Task<IEnumerable<AttendanceNotification>> GetAllAsync();
        Task<AttendanceNotification?> GetByIdAsync(int notificationId);
        Task<IEnumerable<AttendanceNotification>> GetByUserIdAsync(int userId);
        Task<IEnumerable<AttendanceNotification>> GetUnreadByUserIdAsync(int userId);
        Task<bool> AddAsync(AttendanceNotification notification);
        Task<bool> UpdateAsync(AttendanceNotification notification);
        Task<bool> DeleteAsync(int notificationId);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
