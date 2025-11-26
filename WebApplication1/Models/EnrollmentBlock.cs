using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    /// <summary>
    /// Represents a block preventing a student from re-enrolling in a specific class
    /// Created when faculty force-unenrolls a student
    /// </summary>
    public class EnrollmentBlock
    {
        public int BlockId { get; set; }
        
        [Required]
        public int StudId { get; set; }
        
        [Required]
        public int ClassId { get; set; }
        
        [Required]
        public int BlockedByFacId { get; set; }
        
        public string? BlockReason { get; set; }
        
        public DateTime BlockedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? UnblockedAt { get; set; }
        
        public int? UnblockedBy { get; set; }

        // Navigation properties
        public Student? Student { get; set; }
        public Class? Class { get; set; }
        public Faculty? BlockedByFaculty { get; set; }
    }
}
