namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for student basic information used in attendance scanning
/// ✅ ENHANCED: Now includes enrolled classes for better user feedback
/// </summary>
public class StudentInfoDto
{
    public int StudId { get; set; }
    public string StudNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? ProgramId { get; set; }
    public string? ProgramName { get; set; }
    public string? SectionName { get; set; }
    public int? YearLevel { get; set; }
    public bool HasProfile { get; set; }

    /// <summary>
    /// ✅ NEW: List of classes the student is currently enrolled in
    /// Used to display class information after attendance scan
    /// </summary>
    public List<EnrolledClassInfo>? EnrolledClasses { get; set; }
}

/// <summary>
/// ✅ NEW: Information about an enrolled class
/// Used to display class details in attendance responses
/// </summary>
public class EnrolledClassInfo
{
    public int ClassId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? FacultyName { get; set; }
    public string? RoomCode { get; set; }
    public string? Schedule { get; set; }
}
