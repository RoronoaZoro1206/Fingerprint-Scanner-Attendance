namespace Fingerprint_Attendance_System.Models
{
    public enum AttendanceStatus
    {
        Pending,
        Present,
        Late,
        Absent,
        Excused
    }

    public enum ScanType
    {
        In,
        Out
    }

    public class Attendance
    {
        public int AttendId { get; set; }
        public int? ClassId { get; set; }
        public int? StudId { get; set; }
        public DateTime AttendDate { get; set; } = DateTime.Today;
        public DateTime AttendScanTime { get; set; } = DateTime.Now;
        public ScanType AttendType { get; set; }
        public AttendanceStatus AttendStatus { get; set; } = AttendanceStatus.Pending;
        public string? AttendRemark { get; set; }
        public DateTime AttendCreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Class? Class { get; set; }
        public Student? Student { get; set; }
    }
}