namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for faculty report statistics
    /// </summary>
    public class FacultyReportStatsDto
    {
        public int AverageAttendance { get; set; }
        public int LateArrivalsToday { get; set; }
        public int AbsentStudents { get; set; }
        public int TotalStudents { get; set; }
    }
}
