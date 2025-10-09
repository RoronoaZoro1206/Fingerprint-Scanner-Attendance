namespace Fingerprint_Attendance_System.Models
{
    public class Administration
    {
        public int AdminId { get; set; }
        public int UserId { get; set; }
        public string? AdminCode { get; set; }
        public DateTime AdminCreatedAt { get; set; }

        public User? User { get; set; }
    }
}