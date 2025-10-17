namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for student list display with joined user, program, and section data
    /// </summary>
    public class StudentListDto
    {
        public int StudId { get; set; }
        public int UserId { get; set; }
        public string StudName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string StudNumber { get; set; } = string.Empty;
        public int? ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public int? StudYearLevel { get; set; }
        public int? SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public DateTime StudCreatedAt { get; set; }
    }
}
