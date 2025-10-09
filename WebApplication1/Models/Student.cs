namespace Fingerprint_Attendance_System.Models
{
    public class Student
    {
        public int StudId { get; set; }
        public int UserId { get; set; }
        public string StudNumber { get; set; } = string.Empty;
        public int? ProgramId { get; set; }
        public int? StudYearLevel { get; set; }
        public string? StudSection { get; set; }
        public DateTime StudCreatedAt { get; set; }

        public User? User { get; set; }
        public ProgramModel? Program { get; set; }
    }
}