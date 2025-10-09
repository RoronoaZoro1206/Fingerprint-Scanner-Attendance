namespace Fingerprint_Attendance_System.Models
{
    public class ScanLog
    {
        public int ScanId { get; set; }
        public int? UserId { get; set; }
        public DateTime ScanTime { get; set; }
        public bool? ScanSuccess { get; set; }
        public string? ScanLogMessage { get; set; }
        public DateTime ScanCreatedAt { get; set; }

        public User? User { get; set; }
    }
}