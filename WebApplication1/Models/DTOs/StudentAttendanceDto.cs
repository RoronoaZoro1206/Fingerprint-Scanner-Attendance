namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for individual student attendance information
/// Contains all necessary student and attendance details
/// ✅ ENHANCED: Added invalidation fields for schedule change tracking
/// </summary>
public class StudentAttendanceDto
{
    public int StudId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProfileImage { get; set; } = string.Empty;
    public string? TimeIn { get; set; }
    public string? TimeOut { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? AttendId { get; set; }
    
    /// <summary>
    /// ✅ NEW: Indicates if this record was invalidated due to schedule change
    /// Faculty can see invalidated records for audit trail
    /// </summary>
    public bool IsInvalidated { get; set; }
    
    /// <summary>
    /// ✅ NEW: Reason why this record was invalidated (e.g., "Schedule changed from 04:35 to 05:15")
    /// </summary>
    public string? InvalidationReason { get; set; }
    
    /// <summary>
    /// ✅ NEW: Additional remarks for the attendance record
    /// </summary>
    public string? Remark { get; set; }
}
