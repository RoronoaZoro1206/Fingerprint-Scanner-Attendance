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
        public int ClassId { get; set; }
        public int StudId { get; set; }
        public DateTime AttendScanTime { get; set; }
        public ScanType AttendType { get; set; }
        public AttendanceStatus AttendStatus { get; set; }
        public string? AttendRemark { get; set; }
        public DateTime AttendCreatedAt { get; set; }

        public Class? Class { get; set; }
        public Student? Student { get; set; }
    }
}