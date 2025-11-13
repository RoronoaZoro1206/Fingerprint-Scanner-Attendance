namespace Fingerprint_Attendance_System.Models
{
    /// <summary>
    /// Audit trail for faculty attendance modifications
    /// Maps to faculty_attendance_actions table
    /// </summary>
    public class FacultyAttendanceAction
    {
        public int ActionId { get; set; }
        public int FacId { get; set; }
        public int? AttendId { get; set; }
        
        /// <summary>
        /// Type of action: excuse, change_status, add_manual, approve_request, reject_request, bulk_excuse
        /// </summary>
        public string ActionType { get; set; } = string.Empty;
        
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// JSON metadata for additional context (request_id, student_count, etc.)
        /// </summary>
        public string? Metadata { get; set; }
        
        public DateTime ActionTimestamp { get; set; } = DateTime.Now;

        // Navigation properties
        public Faculty? Faculty { get; set; }
        public Attendance? Attendance { get; set; }
    }
}
