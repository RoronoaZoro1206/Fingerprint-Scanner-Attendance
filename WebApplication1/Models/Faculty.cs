namespace Fingerprint_Attendance_System.Models
{
    public class Faculty
    {
        public int FacId { get; set; }
        public int UserId { get; set; }
        public string? FacDepartment { get; set; }
        public string? FacRank { get; set; }
        public DateTime FacCreatedAt { get; set; }

        public User? User { get; set; }
    }
}