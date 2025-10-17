namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for class list display with joined course, room, faculty, and term data
    /// </summary>
    public class ClassListDto
    {
        public int ClassId { get; set; }
        public int? CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int? RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int? FacId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public int? TermId { get; set; }
        public string TermName { get; set; } = string.Empty;
        public DateTime ClassCreatedAt { get; set; }
        public DateTime ClassUpdatedAt { get; set; }
    }
}
