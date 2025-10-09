namespace Fingerprint_Attendance_System.Models
{
    public class ProgramModel
    {
        public int ProgramId { get; set; }
        public string ProgramCode { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public string? ProgramDesc { get; set; }
        public DateTime ProgramCreatedAt { get; set; }
    }
}