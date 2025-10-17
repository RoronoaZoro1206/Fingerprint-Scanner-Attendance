namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for faculty class list with all related data in a single query
    /// </summary>
    public class FacultyClassDto
    {
        public int ClassId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public string TermName { get; set; } = string.Empty;
    }
}
