using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class ScanLog
    {
        public int ScanId { get; set; }
        
        public int? UserId { get; set; }
        
        [Required(ErrorMessage = "Scan time is required")]
        public DateTime ScanTime { get; set; }
        
        public bool? ScanSuccess { get; set; }
        
        [StringLength(500, ErrorMessage = "Log message cannot exceed 500 characters")]
        public string? ScanLogMessage { get; set; }
        
        public DateTime ScanCreatedAt { get; set; }

        public User? User { get; set; }
    }
}