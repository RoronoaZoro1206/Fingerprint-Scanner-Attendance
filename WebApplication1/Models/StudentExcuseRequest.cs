namespace Fingerprint_Attendance_System.Models
{
    /// <summary>
    /// Represents a student-submitted excuse request
    /// Maps to student_excuse_requests table
    /// </summary>
    public class StudentExcuseRequest
    {
        public int ExcuseId { get; set; }
        public int StudId { get; set; }
        public int ClassId { get; set; }
        public DateTime AttendDate { get; set; }
        
        /// <summary>
        /// Type of excuse: sick, emergency, school_activity, family, other
        /// </summary>
        public string ExcuseType { get; set; } = string.Empty;
        
        public string ExcuseReason { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to uploaded medical certificate or supporting document
        /// </summary>
        public string? AttachmentPath { get; set; }
        
        /// <summary>
        /// Request status: pending, approved, rejected
        /// </summary>
        public string Status { get; set; } = "pending";
        
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Faculty member who reviewed the request
        /// </summary>
        public int? ReviewedBy { get; set; }
        
        public DateTime? ReviewedAt { get; set; }
        
        public string? ReviewNotes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Student? Student { get; set; }
        public Class? Class { get; set; }
        public Faculty? Reviewer { get; set; }
    }
}
