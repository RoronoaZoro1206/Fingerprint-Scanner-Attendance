namespace Fingerprint_Attendance_System.Models
{
    public class FingerprintTemplate
    {
        public int FtempId { get; set; }
        public int UserId { get; set; }
        public byte[] FtempData { get; set; } = Array.Empty<byte>();
        public string? FtempName { get; set; }
        public DateTime FtempCreatedAt { get; set; }

        public Student? Student { get; set; }
    }
}