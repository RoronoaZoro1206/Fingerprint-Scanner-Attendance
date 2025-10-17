namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for faculty attendance record summary
    /// </summary>
    public class FacultyAttendanceRecordDto
    {
        public string Date { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }
        public int Excused { get; set; }
    }
}
