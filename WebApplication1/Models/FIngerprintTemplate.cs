using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class FingerprintTemplate
    {
        public int FtempId { get; set; }
        
        [Required(ErrorMessage = "User is required")]
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Fingerprint data is required")]
        public byte[] FtempData { get; set; } = Array.Empty<byte>();
        
        [StringLength(50, ErrorMessage = "Template name cannot exceed 50 characters")]
        public string? FtempName { get; set; }
        
        public DateTime FtempCreatedAt { get; set; }
    }
}
