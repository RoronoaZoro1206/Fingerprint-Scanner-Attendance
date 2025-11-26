using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class StudentIdPool
    {
        public int PoolId { get; set; }
        
        [Required(ErrorMessage = "Student ID code is required")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Student ID code must be between 5 and 20 characters")]
        public string StudentIdCode { get; set; } = string.Empty;
        
        public bool IsUsed { get; set; } = false;
        
        public int? GeneratedBy { get; set; }
        
        public DateTime GeneratedAt { get; set; }
        
        public DateTime? UsedAt { get; set; }
        
        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string? Remarks { get; set; }

        public Administration? GeneratedByAdmin { get; set; }
    }
}