namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for enrolled student information in a class
    /// </summary>
    public class EnrolledStudentDto
    {
        public int EnrollId { get; set; }
        public int StudId { get; set; }
        public string StudNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public int YearLevel { get; set; }
        public DateTime EnrolledAt { get; set; }
        public string ProfileImage { get; set; } = "templates/default_profile.png";
    }
}
