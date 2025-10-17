namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for individual student attendance record
/// Contains date, course, status, and time information
/// </summary>
public class StudentAttendanceRecordDto
{
    public string Date { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TimeIn { get; set; } = string.Empty;
    public string TimeOut { get; set; } = string.Empty;
}
