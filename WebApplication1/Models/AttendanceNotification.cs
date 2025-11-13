namespace Fingerprint_Attendance_System.Models
{
    /// <summary>
    /// Notification for attendance-related events
    /// Maps to attendance_notifications table
    /// </summary>
    public class AttendanceNotification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        
        /// <summary>
        /// Type: excuse_approved, excuse_rejected, attendance_changed, excuse_pending
        /// </summary>
        public string NotificationType { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Related entity type: attendance, excuse_request
        /// </summary>
        public string? RelatedEntityType { get; set; }
        
        public int? RelatedEntityId { get; set; }
        
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public User? User { get; set; }
    }
}
