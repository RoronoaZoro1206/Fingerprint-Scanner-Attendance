using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        
        [Required(ErrorMessage = "Room code is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Room code must be between 1 and 20 characters")]
        public string RoomCode { get; set; } = string.Empty;
        
        public string RoomName => RoomCode; // Alias for consistency
        
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? RoomDescription { get; set; }
    }
}