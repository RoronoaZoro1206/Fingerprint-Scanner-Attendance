using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    /// <summary>
    /// Represents a student-submitted excuse request
    /// Maps to student_excuse_requests table
    /// </summary>
    public class StudentExcuseRequest
    {
        public int ExcuseId { get; set; }
        
        [Required(ErrorMessage = "Student is required")]
        public int StudId { get; set; }
        
        [Required(ErrorMessage = "Class is required")]
        public int ClassId { get; set; }
        
        [Required(ErrorMessage = "Attendance date is required")]
        public DateTime AttendDate { get; set; }
        
        /// <summary>
        /// Type of excuse: sick, emergency, school_activity, family, other
        /// </summary>
        [Required(ErrorMessage = "Excuse type is required")]
        [StringLength(50, ErrorMessage = "Excuse type cannot exceed 50 characters")]
        public string ExcuseType { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Excuse reason is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Excuse reason must be between 10 and 1000 characters")]
        public string ExcuseReason { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to uploaded medical certificate or supporting document
        /// </summary>
        [StringLength(500, ErrorMessage = "Attachment path cannot exceed 500 characters")]
        public string? AttachmentPath { get; set; }
        
        /// <summary>
        /// Request status: pending, approved, rejected
        /// </summary>
        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
        public string Status { get; set; } = "pending";
        
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Faculty member who reviewed the request
        /// </summary>
        public int? ReviewedBy { get; set; }
        
        public DateTime? ReviewedAt { get; set; }
        
        [StringLength(1000, ErrorMessage = "Review notes cannot exceed 1000 characters")]
        public string? ReviewNotes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Student? Student { get; set; }
        public Class? Class { get; set; }
        public Faculty? Reviewer { get; set; }
    }
}
