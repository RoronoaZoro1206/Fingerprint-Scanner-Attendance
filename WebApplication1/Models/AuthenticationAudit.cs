namespace Fingerprint_Attendance_System.Models
{
    /// <summary>
    /// Authentication Audit Log - Universal logging for ALL user authentication events
    /// Stores password logins, biometric logins, failed attempts, and logouts
    /// </summary>
    public class AuthenticationAudit
    {
        public int AuthAuditId { get; set; }
        public int UserId { get; set; }
        public string AuthAction { get; set; } = string.Empty;
        public string? AuthDetails { get; set; }
        public DateTime AuthActionTime { get; set; }
        public string? AuthIpAddress { get; set; }
        public string? AuthUserAgent { get; set; }

        // Navigation property
        public User? User { get; set; }
    }
}
