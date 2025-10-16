namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for individual student attendance information
/// Contains all necessary student and attendance details
/// </summary>
public class StudentAttendanceDto
{
    public int StudId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProfileImage { get; set; } = string.Empty;
    public string? TimeIn { get; set; }
    public string? TimeOut { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? AttendId { get; set; }
}
