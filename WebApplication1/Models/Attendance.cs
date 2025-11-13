namespace Fingerprint_Attendance_System.Models
{
    public enum AttendanceStatus
    {
        Pending,
        Present,
        Late,
        Absent,
        Excused
    }

    public enum ScanType
    {
        In,
        Out
    }

    public class Attendance
    {
        public int AttendId { get; set; }
        public int? ClassId { get; set; }
        public int? StudId { get; set; }
        public DateTime AttendDate { get; set; } = DateTime.Today;
        
        // DEPRECATED: Will be replaced by DailyAttendanceLog.TimeIn
        // Kept for backward compatibility during migration
        public DateTime AttendScanTime { get; set; } = DateTime.Now;
        
        // DEPRECATED: No longer used in daily time in/out system
        // Kept for backward compatibility during migration
        public ScanType AttendType { get; set; }
        
        public AttendanceStatus AttendStatus { get; set; } = AttendanceStatus.Pending;
        public string? AttendRemark { get; set; }
        public DateTime AttendCreatedAt { get; set; } = DateTime.Now;

        // ============================================================
        // NEW PROPERTIES FOR DAILY ATTENDANCE SYSTEM
        // ============================================================
        
        /// <summary>
        /// Foreign key linking to the daily attendance log
        /// This connects class-specific attendance to the student's daily Time In/Out
        /// Database column: daily_attend_id
        /// </summary>
        public int? DailyAttendId { get; set; }

        /// <summary>
        /// Indicates if student was physically present (based on Time In)
        /// vs marked absent by system
        /// Database column: is_present
        /// </summary>
        public bool IsPresent { get; set; } = true;

        // ============================================================
        // NAVIGATION PROPERTIES
        // ============================================================
        
        public Class? Class { get; set; }
        public Student? Student { get; set; }
        
        /// <summary>
        /// Navigation to the daily attendance record
        /// Populated via manual JOIN queries
        /// </summary>
        public DailyAttendanceLog? DailyAttendanceLog { get; set; }
    }
}