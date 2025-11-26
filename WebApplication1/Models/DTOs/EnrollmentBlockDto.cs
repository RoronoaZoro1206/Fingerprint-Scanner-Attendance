namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for displaying blocked/rejected classes to students
    /// </summary>
    public class BlockedClassDto
    {
        public int BlockId { get; set; }
        public int ClassId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string BlockReason { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; }
    }

    // Note: EnrolledStudentDto is defined in EnrolledStudentDto.cs
}
