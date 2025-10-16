namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for enrollment details with all related information
/// Used to avoid N+1 queries by fetching all data in a single query
/// </summary>
public class EnrollmentDetailDto
{
    public int EnrollId { get; set; }
    public int ClassId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
}
