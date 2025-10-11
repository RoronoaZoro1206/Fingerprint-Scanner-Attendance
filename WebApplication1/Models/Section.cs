namespace Fingerprint_Attendance_System.Models
{
    public class Section
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public int ProgramId { get; set; }
        public int? YearLevel { get; set; }
        public DateTime SectionCreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public ProgramModel? Program { get; set; }
    }
}