namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for detailed section information with program details
    /// </summary>
    public class SectionDetailDto
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public string? ProgramCode { get; set; }
        public int? YearLevel { get; set; }
        public int? DeptId { get; set; }
        public string? DeptName { get; set; }
        public DateTime SectionCreatedAt { get; set; }
    }
}
