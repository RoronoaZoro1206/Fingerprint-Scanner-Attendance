namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for student class list with all related data in a single query
    /// </summary>
    public class StudentClassDto
    {
        public int ClassId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int CourseUnits { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public int AttendanceRate { get; set; }
    }
}
