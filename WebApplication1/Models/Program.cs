using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class ProgramModel
    {
        public int ProgramId { get; set; }
        
        [Required(ErrorMessage = "Program code is required")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Program code must be between 2 and 20 characters")]
        public string ProgramCode { get; set; } = string.Empty;  // e.g., BSIT, BSHM
        
        [Required(ErrorMessage = "Program name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Program name must be between 3 and 100 characters")]
        public string ProgramName { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? ProgramDesc { get; set; }
        
        public int? DeptId { get; set; }
        
        public DateTime ProgramCreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public Department? Department { get; set; }
    }
}