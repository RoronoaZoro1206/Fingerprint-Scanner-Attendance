namespace Fingerprint_Attendance_System.Models.DTOs
{
    #region Excuse Request DTOs

    /// <summary>
    /// DTO for submitting a new excuse request
    /// </summary>
    public class SubmitExcuseRequestDto
    {
        public int StudId { get; set; }
        public int ClassId { get; set; }
        public DateTime AttendDate { get; set; }
        public string ExcuseType { get; set; } = string.Empty;
        public string ExcuseReason { get; set; } = string.Empty;
        public IFormFile? Attachment { get; set; }
    }

    /// <summary>
    /// DTO for displaying excuse request in list
    /// </summary>
    public class ExcuseRequestListDto
    {
        public int ExcuseId { get; set; }
        public int StudId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudNumber { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime AttendDate { get; set; }
        public string ExcuseType { get; set; } = string.Empty;
        public string ExcuseReason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string? AttachmentPath { get; set; }
        public bool HasAttachment { get; set; }
    }

    /// <summary>
    /// DTO for reviewing (approve/reject) an excuse request
    /// </summary>
    public class ReviewExcuseRequestDto
    {
        public int ExcuseId { get; set; }
        public bool Approve { get; set; } // true = approve, false = reject
        public string ReviewNotes { get; set; } = string.Empty;
    }

    #endregion

    #region Attendance Modification DTOs

    /// <summary>
    /// DTO for changing attendance status
    /// </summary>
    public class ChangeAttendanceStatusDto
    {
        public int AttendId { get; set; }
        public string NewStatus { get; set; } = string.Empty; // present, late, absent, excused
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for adding manual attendance record
    /// </summary>
    public class ManualAttendanceDto
    {
        public int ClassId { get; set; }
        public int StudId { get; set; }
        public DateTime AttendDate { get; set; }
        public string Status { get; set; } = "present"; // present, late, excused
        public TimeSpan? TimeIn { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for bulk excuse operation
    /// </summary>
    public class BulkExcuseDto
    {
        public int ClassId { get; set; }
        public DateTime AttendDate { get; set; }
        public List<int> StudentIds { get; set; } = new();
        public string ExcuseType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for excusing a single attendance record
    /// </summary>
    public class ExcuseAttendanceDto
    {
        public int AttendId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Result of a faculty attendance action
    /// </summary>
    public class AttendanceActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public int? AffectedRecords { get; set; }
        public DateTime ActionTimestamp { get; set; }
    }

    /// <summary>
    /// Overview statistics for faculty attendance management
    /// </summary>
    public class FacultyAttendanceOverviewDto
    {
        public int TotalStudents { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        public int ExcusedToday { get; set; }
        public int PendingExcuses { get; set; }
    }

    /// <summary>
    /// Detailed attendance record with modification permissions
    /// </summary>
    public class AttendanceDetailDto
    {
        public int AttendId { get; set; }
        public int StudId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AttendDate { get; set; }
        public DateTime? ScanTime { get; set; }
        public string? Remarks { get; set; }
        public bool CanModify { get; set; } // Based on date and faculty permissions
        public bool HasPendingExcuse { get; set; }
    }

    #endregion

    #region Faculty Action History DTOs

    /// <summary>
    /// DTO for displaying faculty action history
    /// </summary>
    public class FacultyActionHistoryDto
    {
        public int ActionId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime ActionTimestamp { get; set; }
        public string? StudentNumber { get; set; }
        public string? CourseCode { get; set; }
        public int? AffectedStudents { get; set; } // For bulk operations
    }

    #endregion

    #region Notification DTOs

    /// <summary>
    /// DTO for attendance notification
    /// </summary>
    public class AttendanceNotificationDto
    {
        public int NotificationId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    #endregion
}
