namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for complete student attendance data
/// Combines statistics and recent attendance records
/// </summary>
public class StudentAttendanceDataDto
{
    public StudentAttendanceStatsDto Statistics { get; set; } = new();
    public List<StudentAttendanceRecordDto> Records { get; set; } = new();
}
