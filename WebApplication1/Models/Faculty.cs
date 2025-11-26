using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class Faculty
    {
        public int FacId { get; set; }
        
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }
        
        public int? DeptId { get; set; }
        
        [StringLength(50, ErrorMessage = "Rank cannot exceed 50 characters")]
        public string? FacRank { get; set; }
        
        public DateTime FacCreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User? User { get; set; }
        public Department? Department { get; set; }

        // Computed property for display name
        public string FacName => User != null 
            ? $"{User.UserFirstName} {User.UserLastName}".Trim() 
            : string.Empty;
    }
}