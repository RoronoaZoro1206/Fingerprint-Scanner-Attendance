namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for Audit Log display
    /// Unified structure for both administration and authentication audit logs
    /// </summary>
    public class AuditLogDto
    {
        public string AudId { get; set; } = string.Empty;         // e.g., "ADM-5" or "AUTH-123"
        public int AdminId { get; set; }
        public string UserName { get; set; } = string.Empty;      // ✅ Renamed from AdminName
        public string UserRole { get; set; } = string.Empty;      // ✅ New field (Admin/Faculty/Student)
        public string AudAction { get; set; } = string.Empty;
        public string AudDetails { get; set; } = string.Empty;
        public DateTime AudActionTime { get; set; }
        public string AudActionTimeFormatted { get; set; } = string.Empty;
        public string LogType { get; set; } = string.Empty;
    }
}
