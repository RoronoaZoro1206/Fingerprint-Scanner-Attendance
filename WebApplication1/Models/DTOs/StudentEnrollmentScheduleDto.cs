namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for student enrollment with full schedule details
/// Used for schedule conflict detection during enrollment
/// </summary>
public class StudentEnrollmentScheduleDto
{
    public int ClassId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string DayOfWeek { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
