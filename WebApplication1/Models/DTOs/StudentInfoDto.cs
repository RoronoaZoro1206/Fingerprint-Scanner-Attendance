namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for student information with program details
/// </summary>
public class StudentInfoDto
{
    public int StudId { get; set; }
    public string StudNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public int? YearLevel { get; set; }
    public bool HasProfile { get; set; }
}
