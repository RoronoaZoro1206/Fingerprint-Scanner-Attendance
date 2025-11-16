namespace Fingerprint_Attendance_System.Models
{
    /// <summary>
    /// Represents a student's single daily Time In/Time Out record
    /// Independent of specific class attendance
    /// Maps to daily_attendance_log table in PostgreSQL
    /// </summary>
    public class DailyAttendanceLog
    {
        /// <summary>
        /// Primary key for daily attendance record
        /// Database column: daily_attend_id
        /// </summary>
        public int DailyAttendId { get; set; }

        /// <summary>
        /// Foreign key to students table
        /// Database column: stud_id
        /// </summary>
        public int StudId { get; set; }

        /// <summary>
        /// ✅ NEW: Foreign key to classes table
        /// Database column: class_id
        /// Links attendance to specific class - prevents duplicate time-ins for same class
        /// </summary>
        public int? ClassId { get; set; }

        /// <summary>
        /// Date of attendance (without time component)
        /// Database column: attend_date
        /// </summary>
        public DateTime AttendDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Timestamp when student first scanned fingerprint for the day
        /// NULL = Student hasn't timed in yet
        /// Database column: time_in
        /// </summary>
        public DateTime? TimeIn { get; set; }

        /// <summary>
        /// Timestamp when student scanned fingerprint to leave
        /// NULL = Student hasn't timed out yet
        /// Database column: time_out
        /// </summary>
        public DateTime? TimeOut { get; set; }

        /// <summary>
        /// Status of daily attendance record
        /// Database column: status
        /// Possible values:
        /// - "active": Only TimeIn recorded (student is currently in school)
        /// - "completed": Both TimeIn and TimeOut recorded (day completed)
        /// - "incomplete": Forgot to time out (flagged by automated job)
        /// </summary>
        public string Status { get; set; } = "active";

        /// <summary>
        /// Flag indicating student forgot to time out previous day
        /// Used for admin review and student notification
        /// Database column: forgot_timeout
        /// </summary>
        public bool ForgotTimeout { get; set; } = false;

        /// <summary>
        /// Optional notes or remarks for this daily attendance
        /// Database column: remarks
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// When this record was first created
        /// Database column: created_at
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// When this record was last updated
        /// Database column: updated_at
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ============================================================
        // NAVIGATION PROPERTIES (for in-memory relationships)
        // ============================================================

        /// <summary>
        /// Navigation to the student who owns this attendance record
        /// Populated via manual JOIN queries
        /// </summary>
        public Student? Student { get; set; }

        /// <summary>
        /// Navigation to all class-specific attendance records linked to this daily log
        /// Populated via manual JOIN queries
        /// </summary>
        public ICollection<Attendance>? ClassAttendances { get; set; }

        /// <summary>
        /// ✅ NEW: Navigation property to class
        /// </summary>
        public Class? Class { get; set; }

        // ============================================================
        // COMPUTED PROPERTIES
        // ============================================================

        /// <summary>
        /// Calculate total hours spent in school
        /// Returns NULL if TimeOut is not recorded yet
        /// </summary>
        public decimal? HoursInSchool
        {
            get
            {
                if (TimeIn.HasValue && TimeOut.HasValue)
                {
                    var duration = TimeOut.Value - TimeIn.Value;
                    return (decimal)duration.TotalHours;
                }
                return null;
            }
        }

        /// <summary>
        /// Check if student can time in
        /// True if: no time_in recorded OR previous day completed
        /// </summary>
        public bool CanTimeIn => !TimeIn.HasValue || (TimeOut.HasValue && Status == "completed");

        /// <summary>
        /// Check if student can time out
        /// True if: time_in recorded AND time_out not recorded
        /// </summary>
        public bool CanTimeOut => TimeIn.HasValue && !TimeOut.HasValue && Status == "active";
    }
}
