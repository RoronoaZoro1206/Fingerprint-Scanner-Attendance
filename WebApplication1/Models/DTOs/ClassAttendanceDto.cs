namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for class attendance with course information and student attendance details
/// Used to avoid N+1 queries by fetching all data in a single query
/// </summary>
public class ClassAttendanceDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public List<StudentAttendanceDto> Students { get; set; } = new();
}
