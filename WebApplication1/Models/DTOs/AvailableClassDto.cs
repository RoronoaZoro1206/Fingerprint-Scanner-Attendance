namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for available classes that a student can enroll in
/// Includes all necessary information without requiring multiple repository calls
/// </summary>
public class AvailableClassDto
{
    public int ClassId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
}
