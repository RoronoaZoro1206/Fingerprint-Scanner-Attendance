namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for fingerprint scan response
    /// Used for both time in and time out operations
    /// </summary>
    public class AttendanceScanResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "time_in", "time_out", "already_completed"
        public DateTime? Timestamp { get; set; }
        public StudentInfoDto? Student { get; set; }
        public int? DailyAttendId { get; set; }
        public string? IncompleteWarning { get; set; } // If student forgot to time out previously
        public string? IncompleteRecordsWarning { get; set; } // ? NEW: Phase 5 - Detailed warning message
    }

    /// <summary>
    /// DTO for daily attendance record details
    /// Used for displaying student's daily attendance status
    /// </summary>
    public class DailyAttendanceDetailDto
    {
        public int DailyAttendId { get; set; }
        public int StudId { get; set; }
        public string StudNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime AttendDate { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool ForgotTimeout { get; set; }
        public decimal? HoursInSchool { get; set; }
        public List<ClassAttendanceStatusDto>? ClassStatuses { get; set; }
    }

    /// <summary>
    /// DTO for class-specific attendance status
    /// Shows how time in affected each class (late/present/absent)
    /// </summary>
    public class ClassAttendanceStatusDto
    {
        public int ClassId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public string Status { get; set; } = string.Empty; // "Present", "Late", "Absent"
        public string? RoomCode { get; set; }
    }

    /// <summary>
    /// DTO for incomplete attendance records (forgot to time out)
    /// Used for admin reports and student notifications
    /// </summary>
    public class IncompleteAttendanceDto
    {
        public int DailyAttendId { get; set; }
        public int StudId { get; set; }
        public string StudNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime AttendDate { get; set; }
        public DateTime TimeIn { get; set; }
        public int DaysAgo { get; set; }
        public string DaysAgoText { get; set; } = string.Empty; // "1 day ago", "2 days ago"
    }

    /// <summary>
    /// DTO for incomplete record details with formatted days ago text
    /// Used by MissedTimeOutService in Phase 5
    /// </summary>
    public class IncompleteRecordDto
    {
        public int DailyAttendId { get; set; }
        public int StudId { get; set; }
        public string StudNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime AttendDate { get; set; }
        public DateTime TimeIn { get; set; }
        public string DaysAgo { get; set; } = string.Empty; // "1 day(s) ago"
    }

    /// <summary>
    /// DTO for daily attendance summary
    /// Used for dashboard statistics
    /// </summary>
    public class DailyAttendanceSummaryDto
    {
        public DateTime Date { get; set; }
        public int TotalStudents { get; set; }
        public int TimedIn { get; set; }
        public int TimedOut { get; set; }
        public int StillOnCampus { get; set; }
        public int IncompleteRecords { get; set; }
    }

    /// <summary>
    /// DTO for time in request
    /// Used when student scans fingerprint to time in
    /// </summary>
    public class TimeInRequestDto
    {
        public int StudId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// DTO for time out request
    /// Used when student scans fingerprint to time out
    /// </summary>
    public class TimeOutRequestDto
    {
        public int StudId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// DTO for attendance status check
    /// Used to check if student can time in/out
    /// </summary>
    public class AttendanceStatusDto
    {
        public bool CanTimeIn { get; set; }
        public bool CanTimeOut { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
