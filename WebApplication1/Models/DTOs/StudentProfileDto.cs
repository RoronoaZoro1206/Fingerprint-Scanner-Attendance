namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for complete student profile with user, program, and section data
    /// </summary>
    public class StudentProfileDto
    {
        public int UserId { get; set; }
        public string UserProfileImage { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string? UserMiddleName { get; set; }
        public string UserLastName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? UserPhone { get; set; }
        public string? UserGender { get; set; }
        public string? UserBirthdate { get; set; }
        public string? UserAddress { get; set; }
        public int? StudId { get; set; }
        public string? StudNumber { get; set; }
        public int? ProgramId { get; set; }
        public string? ProgramName { get; set; }
        public int? StudYearLevel { get; set; }
        public int? SectionId { get; set; }
        public string? SectionName { get; set; }
        public bool ProfileSubmitted { get; set; }
        public string? ProfileSubmittedAt { get; set; }
        public bool HasStudentProfile { get; set; }
    }
}
