using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        
        [Required(ErrorMessage = "Course code is required")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Course code must be between 2 and 20 characters")]
        public string CourseCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Course name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Course name must be between 3 and 100 characters")]
        public string CourseName { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? CourseDesc { get; set; }
        
        [Required(ErrorMessage = "Course units is required")]
        [Range(1, 12, ErrorMessage = "Course units must be between 1 and 12")]
        public int CourseUnits { get; set; }
        
        public int? ProgramId { get; set; }
        
        public DateTime CourseCreatedAt { get; set; }

        public ProgramModel? Program { get; set; }
    }
}