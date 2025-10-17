namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for section list display with joined program data
    /// </summary>
    public class SectionListDto
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public int? YearLevel { get; set; }
        public DateTime SectionCreatedAt { get; set; }
    }
}
