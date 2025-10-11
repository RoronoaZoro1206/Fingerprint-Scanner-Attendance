namespace Fingerprint_Attendance_System.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string RoomName => RoomCode; // Alias for consistency
        public string? RoomDescription { get; set; }
    }
}