namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for course with program information
/// Used to avoid N+1 queries by fetching all data in a single query
/// </summary>
public class CourseDetailDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? CourseDesc { get; set; }
    public int CourseUnits { get; set; }
    public int? ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
}
