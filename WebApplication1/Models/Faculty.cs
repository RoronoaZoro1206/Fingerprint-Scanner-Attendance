namespace Fingerprint_Attendance_System.Models
{
    public class Faculty
    {
        public int FacId { get; set; }
        public int UserId { get; set; }
        public int? DeptId { get; set; }
        public string? FacRank { get; set; }
        public DateTime FacCreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User? User { get; set; }
        public Department? Department { get; set; }
    }
}