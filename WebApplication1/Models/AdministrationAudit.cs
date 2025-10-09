namespace Fingerprint_Attendance_System.Models
{
    public class AdministrationAudit
    {
        public int AudId { get; set; }
        public int AdminId { get; set; }
        public string AudAction { get; set; } = string.Empty;
        public string? AudDetails { get; set; }
        public DateTime AudActionTime { get; set; }

        public Administration? Admin { get; set; }
    }
}