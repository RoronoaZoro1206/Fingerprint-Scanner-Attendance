namespace Fingerprint_Attendance_System.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string? CourseDesc { get; set; }
        public int CourseUnits { get; set; }
        public int? ProgramId { get; set; }
        public DateTime CourseCreatedAt { get; set; }

        public ProgramModel? Program { get; set; }
    }
}