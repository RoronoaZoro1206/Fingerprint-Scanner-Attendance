using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class Student
    {
        public int StudId { get; set; }
        
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Student number is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Student number must be between 1 and 20 characters")]
        public string StudNumber { get; set; } = string.Empty;
        
        public int? ProgramId { get; set; }
        
        [Range(1, 6, ErrorMessage = "Year level must be between 1 and 6")]
        public int? StudYearLevel { get; set; }
        
        public int? SectionId { get; set; }
        
        public bool ProfileSubmitted { get; set; } = false;
        
        public DateTime? ProfileSubmittedAt { get; set; }
        
        public DateTime StudCreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User? User { get; set; }
        public ProgramModel? Program { get; set; }
        public Section? Section { get; set; }
    }
}