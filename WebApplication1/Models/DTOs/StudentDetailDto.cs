namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for detailed student information with user details
    /// </summary>
    public class StudentDetailDto
    {
        public int StudId { get; set; }
        public int UserId { get; set; }
        public string StudNumber { get; set; } = string.Empty;
        public int? ProgramId { get; set; }
        public int? StudYearLevel { get; set; }
        public int? SectionId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string? UserMiddleName { get; set; }
        public string UserLastName { get; set; } = string.Empty;
        public string? UserPhone { get; set; }
        public string? ProgramName { get; set; }
        public string? SectionName { get; set; }
        public DateTime StudCreatedAt { get; set; }
    }
}
