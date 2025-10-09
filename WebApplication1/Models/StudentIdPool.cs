namespace Fingerprint_Attendance_System.Models
{
    public class StudentIdPool
    {
        public int PoolId { get; set; }
        public string StudentIdCode { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
        public int? GeneratedBy { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public string? Remarks { get; set; }

        public Administration? GeneratedByAdmin { get; set; }
    }
}