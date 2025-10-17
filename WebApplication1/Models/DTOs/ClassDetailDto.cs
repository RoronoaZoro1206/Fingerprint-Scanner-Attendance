namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for detailed class information with all related data
    /// </summary>
    public class ClassDetailDto
    {
        public int ClassId { get; set; }
        public int? CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int? RoomId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string? RoomDescription { get; set; }
        public int? FacId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public int? TermId { get; set; }
        public string TermName { get; set; } = string.Empty;
        public DateTime? TermStartDate { get; set; }
        public DateTime? TermEndDate { get; set; }
        public DateTime ClassCreatedAt { get; set; }
        public DateTime ClassUpdatedAt { get; set; }
    }
}
